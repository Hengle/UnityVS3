using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Packages.VisualScripting.Editor.Elements;
using Packages.VisualScripting.Editor.Stencils;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.Compilation;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEditor.VisualScripting.Model.Compilation;
using UnityEngine;
using UnityEngine.Assertions;
using Assembly = System.Reflection.Assembly;
using CompilationOptions = UnityEngine.VisualScripting.CompilationOptions;
using Component = UnityEngine.Component;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    class EcsStencil : ClassStencil, IHasOrderedStacks
    {
        public override IBuilder Builder => EcsBuilder.Instance;

        DragNDropEcsHandler m_NDropHandler;
        public override IExternalDragNDropHandler DragNDropHandler => m_NDropHandler ?? (m_NDropHandler = new DragNDropEcsHandler());

        [UsedImplicitly]
        public bool UseJobSystem;

        public override IEnumerable<string> PropertiesVisibleInGraphInspector()
        {
            yield return nameof(UseJobSystem);
        }

        public Dictionary<IFunctionModel, HashSet<ComponentQueryDeclarationModel>> entryPointsToQueries =
            new Dictionary<IFunctionModel, HashSet<ComponentQueryDeclarationModel>>();

        public override void PreProcessGraph(VSGraphModel graphModel)
        {
            entryPointsToQueries.Clear();
            new PortInitializationTraversal
            {
                Callbacks =
                {
                    n =>
                    {
                        if (n is IIteratorStackModel iterator)
                        {
                            var key = iterator.OwningFunctionModel ?? iterator;
                            if (key == null || !iterator.ComponentQueryDeclarationModel)
                                return;
                            if (!entryPointsToQueries.TryGetValue(key, out var queries))
                            {
                                queries = new HashSet<ComponentQueryDeclarationModel> { iterator.ComponentQueryDeclarationModel };
                                entryPointsToQueries.Add(key, queries);
                            }
                            else
                                queries.Add(iterator.ComponentQueryDeclarationModel);
                        }
                    }
                }
            }.VisitGraph(graphModel);
        }

        public List<VSGraphAssetModel> UpdateAfter = new List<VSGraphAssetModel>();
        public List<VSGraphAssetModel> UpdateBefore = new List<VSGraphAssetModel>();

        ISearcherDatabaseProvider m_SearcherDatabaseProvider;
        ISearcherFilterProvider m_SearcherFilterProvider;
        Dictionary<INodeModel, IEnumerable<ComponentDefinition>> m_ComponentDefinitions;

        internal Dictionary<INodeModel, IEnumerable<ComponentDefinition>> ComponentDefinitions => m_ComponentDefinitions
            ?? (m_ComponentDefinitions = new Dictionary<INodeModel, IEnumerable<ComponentDefinition>>());

        [MenuItem("Assets/Create/Visual Script/ECS Graph")]
        public static void CreateEcsGraph()
        {
           VseWindow.CreateGraphAsset<EcsStencil>();
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ?? (m_SearcherDatabaseProvider = new EcsSearcherDatabaseProvider(this));
        }

        public override ISearcherFilterProvider GetSearcherFilterProvider()
        {
            return m_SearcherFilterProvider ?? (m_SearcherFilterProvider = new EcsSearcherFilterProvider(this));
        }

        public override TypeHandle GetThisType()
        {
            return typeof(JobComponentSystem).GenerateTypeHandle(this);
        }

        public override ITranslator CreateTranslator()
        {
            return new RoslynEcsTranslator(this);
        }

        public override IBlackboardProvider GetBlackboardProvider()
        {
            return m_BlackboardProvider ?? (m_BlackboardProvider = new BlackboardEcsProvider(this));
        }

        protected override GraphContext CreateGraphContext()
        {
            return new EcsGraphContext();
        }

        internal void ClearComponentDefinitions()
        {
            m_ComponentDefinitions = new Dictionary<INodeModel, IEnumerable<ComponentDefinition>>();
        }

        public override void OnCompilationSucceeded(VSGraphModel graphModel, CompilationResult results)
        {
            World world = World.Active;
            if (!EditorApplication.isPlaying || world == null)
                return;

            ComponentSystemBase mgr = world.Systems
                .FirstOrDefault(m => m.GetType().Name == graphModel.TypeName);

            if (mgr != null)
            {
                // AFAIK the current frame's loop is already scheduled, so deleting the manager will flag
                // it as nonexistent but won't remove it. to avoid a console error about it, disable it first
                mgr.Enabled = false;
                world.DestroySystem(mgr);
            }

            if (graphModel.State == ModelState.Disabled)
                return;

            Type t = LiveCompileGraph(graphModel, results);
            if (t == null)
                return;

            var newSystem = world.CreateSystem(t);

            var groups = TypeManager.GetSystemAttributes(t, typeof(UpdateInGroupAttribute));
            if (groups.Length == 0) // default group is Simulation
            {
                var groupSystem = world.GetExistingSystem<SimulationSystemGroup>();
                groupSystem.AddSystemToUpdateList(newSystem);
                groupSystem.SortSystemUpdateList();
            }
            else
            {
                for (int g = 0; g < groups.Length; ++g)
                {
                    var updateInGroupAttribute = (UpdateInGroupAttribute)groups[g];
                    Assert.IsNotNull(updateInGroupAttribute);
                    var groupSystem = (ComponentSystemGroup)world.GetExistingSystem(updateInGroupAttribute.GroupType);
                    groupSystem.AddSystemToUpdateList(newSystem);
                    groupSystem.SortSystemUpdateList();
                }
            }

            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
        }

        public static Type LiveCompileGraph(VSGraphModel graphModel, CompilationResult results, bool includeVscriptingAssemblies = false)
        {
            VseUtility.RemoveLogEntries();
            var graphModelTypeName = graphModel.TypeName;

            string src = results.sourceCode[0];

            // TODO: refactor
            Assembly[] assemblies = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                where !domainAssembly.IsDynamic &&
                    (includeVscriptingAssemblies || !domainAssembly.FullName.Contains("VisualScripting")) && // TODO: hack to avoid dll lock during test exec
                    domainAssembly.Location != ""
                select domainAssembly).ToArray();

            Script<object> s = CSharpScript.Create(src, ScriptOptions.Default.AddReferences(assemblies));

            bool abort = false;
            foreach (Diagnostic diagnostic in s.Compile().Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                abort = true;
                VseUtility.LogSticky(LogType.Error, LogOption.NoStacktrace, diagnostic.ToString());
            }

            if (abort)
                return null;

            string newString = $"typeof({graphModelTypeName})";
            s = s.ContinueWith(newString);

            Type t = s.RunAsync().Result.ReturnValue as Type;

            // The type manager now complains that ' All ComponentType must be known at compile time'.
            // re-initialize it so it finds the newly compiled type
            TypeManager.Shutdown();

            TypeManager.Initialize();

            return t;
        }

        public override string GetSourceFilePath(VSGraphModel graphModel)
        {
            return Path.Combine(ModelUtility.GetAssemblyOutputDirectory(), graphModel.TypeName + ".cs");
        }

        public override void RegisterReducers(Store store)
        {
            EcsReducers.Register(store);
        }

        static Dictionary<TypeHandle, Type> s_TypeToConstantNodeModelTypeCache;
        public override Type GetConstantNodeModelType(TypeHandle typeHandle)
        {
            if (s_TypeToConstantNodeModelTypeCache == null)
            {
                s_TypeToConstantNodeModelTypeCache = new Dictionary<TypeHandle, Type>
                {
                    { typeof(float2).GenerateTypeHandle(this), typeof(Float2ConstantModel) },
                    { typeof(float3).GenerateTypeHandle(this), typeof(Float3ConstantModel) },
                    { typeof(float4).GenerateTypeHandle(this), typeof(Float4ConstantModel) },
                };
            }

            return s_TypeToConstantNodeModelTypeCache.TryGetValue(typeHandle, out var type)
                ? type
                : base.GetConstantNodeModelType(typeHandle);
        }

        public static bool IsValidGameObjectComponentType(Type type)
        {
            return type != null && typeof(Component).IsAssignableFrom(type) && !typeof(ComponentDataProxyBase).IsAssignableFrom(type);
        }
    }

    class EcsGraphContext : GraphContext
    {
        protected override IMemberConstrainer CreateMemberConstrainer()
        {
            return new EcsConstrainer(TypeHandleSerializer);
        }

        protected override TypeHandleSerializer CreateTypeHandleSerializer()
        {
            return new TypeHandleSerializer(new CSharpTypeSerializer(new Dictionary<string, string>
            {
                {
                    "Unity.Transforms.Position, Unity.Transforms, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                    "Unity.Transforms.Translation, Unity.Transforms, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"
                }
            }), new GraphTypeSerializer());
        }
    }

    class EcsConstrainer : IMemberConstrainer
    {
        readonly HashSet<MemberInfoValue> m_BlacklistedMembers;

        public EcsConstrainer(ITypeHandleSerializer serializer)
        {
            m_BlacklistedMembers = new HashSet<MemberInfoValue>();

            var asm = typeof(float3).Assembly;
            foreach (var type in asm.GetExportedTypes())
            {
                if (type.Name.StartsWith("float"))
                {
                    foreach (var property in type.GetProperties())
                    {
                        if (property.GetCustomAttribute<EditorBrowsableAttribute>()?.State == EditorBrowsableState.Never)
                            m_BlacklistedMembers.Add(property.ToMemberInfoValue(serializer));
                    }
                }
            }
        }

        public bool MemberAllowed(MemberInfoValue value)
        {
            return !m_BlacklistedMembers.Contains(value);
        }
    }

    class EcsBuilder : IBuilder
    {
        public static IBuilder Instance = new EcsBuilder();
        public void Build(IEnumerable<GraphAssetModel> vsGraphAssetModels, Action<string, CompilerMessage[]> roslynCompilationOnBuildFinished)
        {
            VseUtility.RemoveLogEntries();
            foreach (GraphAssetModel vsGraphAssetModel in vsGraphAssetModels)
            {
                VSGraphModel graphModel = (VSGraphModel)vsGraphAssetModel.GraphModel;
                var t = graphModel.Stencil.CreateTranslator();

                try
                {
                    // important for codegen, otherwise most of it will be skipped
                    graphModel.Stencil.PreProcessGraph(graphModel);
                    var result = t.TranslateAndCompile(graphModel, AssemblyType.Source, CompilationOptions.Default);
                    var graphAssetPath = AssetDatabase.GetAssetPath(vsGraphAssetModel);
                    foreach (var error in result.errors)
                        VseUtility.LogSticky(LogType.Error, LogOption.None, error.ToString(), graphAssetPath, vsGraphAssetModel.GetInstanceID());
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }

            AssetDatabase.Refresh();
        }
    }
}
