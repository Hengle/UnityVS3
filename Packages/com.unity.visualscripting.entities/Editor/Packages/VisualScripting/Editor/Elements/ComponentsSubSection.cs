using System;
using System.Collections.Generic;
using Packages.VisualScripting.Editor.Redux.Actions;
using Packages.VisualScripting.Editor.Stencils;
using Unity.Entities;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.SmartSearch;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Blackboard = UnityEditor.VisualScripting.Editor.Blackboard;

namespace Packages.VisualScripting.Editor.Elements
{
    class ComponentsSubSection : SortableExpandableRow
    {
        Stencil m_Stencil;

        public ComponentsSubSection(Stencil stencil,
            IReadOnlyCollection<ComponentQueryDeclarationModel> componentQueryDeclarationModels,
            Blackboard blackboard)
            : base("Components",
                graphElementModel: null,
                store: blackboard.Store,
                parentElement: null,
                rebuildCallback: null,
                canAcceptDrop: null)
        {
            m_Stencil = stencil;

            name = "componentsSection";
            userData = name;

            AddToClassList("subSection");

            int nbRows = 0;

            State state = blackboard.Store.GetState();

            foreach (var componentQueryDeclarationModel in componentQueryDeclarationModels)
            {
                var componentQuery = new ComponentQuery(componentQueryDeclarationModel, Store, blackboard.Rebuild);

                var componentQueryDeclarationExpandableRow = new ExpandableRow("")
                {
                    name = "componentsSectionComponentQuery",
                    viewDataKey = "ComponentsSubSection/" + componentQueryDeclarationModel.GetId(),
                    userData = $"ComponentsSubSection/{componentQueryDeclarationModel.name}",
                };
                componentQueryDeclarationExpandableRow.Sortable = true;
                componentQueryDeclarationExpandableRow.ExpandableRowTitleContainer.AddManipulator(new Clickable(() => { }));
                componentQueryDeclarationExpandableRow.ExpandableRowTitleContainer.Add(componentQuery);
                componentQueryDeclarationExpandableRow.ExpandableRowTitleContainer.Add(new Label($"({(componentQueryDeclarationModel.Query?.Components?.Count ?? 0)})") { name = "count" });
                componentQueryDeclarationExpandableRow.ExpandableRowTitleContainer.Add(new Button(() => { AddComponentToQuery(componentQueryDeclarationModel); }) { name = "addComponentButton", text = "+" });
                var rowName = $"{ComponentQueriesRow.BlackboardEcsProviderTypeName}/{GetType().Name}/{componentQueryDeclarationModel}";
                componentQueryDeclarationExpandableRow.OnExpanded += e =>
                    Store.GetState().EditorDataModel?.ExpandBlackboardRowsUponCreation(new[] { rowName }, e);
                if (state.EditorDataModel.ShouldExpandBlackboardRowUponCreation(rowName))
                    componentQueryDeclarationExpandableRow.Expanded = true;

                ExpandedContainer.Add(componentQueryDeclarationExpandableRow);
                blackboard.GraphVariables.Add(componentQuery);

                componentQueryDeclarationModel.ExpandOnCreateUI = false;

                nbRows += AddRows(componentQueryDeclarationModel, componentQueryDeclarationExpandableRow.ExpandedContainer);
            }

            viewDataKey = "blackboardComponentsSection";

            OnExpanded = e => Store.GetState().EditorDataModel?.ExpandBlackboardRowsUponCreation(
                new[] { $"{ComponentQueriesRow.BlackboardEcsProviderTypeName}/{GetType().Name}" }, e);

            SectionTitle.text += " (" + nbRows + ")";
        }

        int AddRows(ComponentQueryDeclarationModel componentQueryDeclarationModel, ExpandedContainer expandedContainer)
        {
            QueryContainer query = componentQueryDeclarationModel.Query;

            if (query?.RootGroup == null)
                return 0;

            return AddGroupComponentRows(query, query.RootGroup, expandedContainer, componentQueryDeclarationModel);
        }

        int AddGroupComponentRows(QueryContainer query, QueryGroup queryRootGroup, ExpandedContainer expandedContainer, ComponentQueryDeclarationModel componentQueryDeclarationModel)
        {
            int nbRows = 0;

            foreach (QueryGroup subGroup in query.GetSubGroups(queryRootGroup))
                nbRows += AddGroupComponentRows(query, subGroup, expandedContainer, componentQueryDeclarationModel);

            foreach (QueryComponent component in query.GetComponentsInQuery(queryRootGroup))
                nbRows += AddComponentRow(component.Component, componentQueryDeclarationModel, expandedContainer);

            return nbRows;
        }

        int AddComponentRow(ComponentDefinition component,
            ComponentQueryDeclarationModel componentQueryDeclarationModel,
            ExpandedContainer expandedContainer)
        {
            Assert.IsNotNull(expandedContainer);

            expandedContainer.Add(new ComponentRow(componentQueryDeclarationModel,
                component,
                m_Stencil,
                Store,
                expandedContainer,
                OnDeleteComponent,
                OnUsageChanged));

            return 1;
        }

        public void AddComponentToQuery(ComponentQueryDeclarationModel componentQueryDeclarationModel)
        {
            SearcherService.ShowTypes(
                m_Stencil,
                Event.current.mousePosition, (t, i) =>
                {
                    var resolvedType = t.Resolve(m_Stencil);
                    ComponentDefinitionFlags creationFlags =
                        (typeof(ISharedComponentData).IsAssignableFrom(resolvedType))
                            ? ComponentDefinitionFlags.Shared
                            : 0;
                    Store.Dispatch(new AddComponentToQueryAction(componentQueryDeclarationModel,
                        t,
                        creationFlags));
                },
                GetComponentsSearcherFilter(m_Stencil)
            );
        }

        static EcsSearcherFilter GetComponentsSearcherFilter(Stencil stencil)
        {
            return new EcsSearcherFilter(SearcherContext.Type)
                .WithComponentData(stencil)
                .WithGameObjectComponents(stencil)
                .WithSharedComponentData(stencil);
        }

        void OnUsageChanged(EventBase evt)
        {
            if (evt.eventTypeId == ChangeEvent<bool>.TypeId())
            {
                var e = (ChangeEvent<bool>)evt;
                var componentRow = ((VisualElement)evt.target).GetFirstOfType<ComponentRow>();
                if (componentRow?.Component == null)
                    return;

                Store.Dispatch(new ChangeComponentUsageAction((ComponentQueryDeclarationModel)componentRow.GraphElementModel, componentRow.Component, e.newValue));
            }
        }

        void OnDeleteComponent(EventBase evt)
        {
            var componentRow = ((VisualElement)evt.target).GetFirstOfType<ComponentRow>();
            if (componentRow?.Component == null)
                return;

            Store.Dispatch(new RemoveComponentFromQueryAction((ComponentQueryDeclarationModel)componentRow.GraphElementModel, componentRow.Component));
        }

        protected override bool AcceptDrop(GraphElement element)
        {
            return false;
        }

        public override void DuplicateRow(GraphElement selectedElement, SortableExpandableRow insertTargetElement, bool insertAtEnd)
        {
        }

        protected override void MoveRow(GraphElement selectedElement, SortableExpandableRow insertTargetElement, bool insertAtEnd)
        {
        }
    }
}
