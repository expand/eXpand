﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.ExpressApp.Dashboard.BusinessObjects;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.Utils.Linq;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.ExpressApp.Dashboard.Controllers {
    public partial class DashboardNavigationController : WindowController, IModelExtender {
        Dictionary<ChoiceActionItem, DashboardDefinition> _dashboardActions;
        ShowNavigationItemController _navigationController;

        public DashboardNavigationController() {
            TargetWindowType = WindowType.Main;
        }

        protected Dictionary<ChoiceActionItem, DashboardDefinition> DashboardActions => _dashboardActions ??= new Dictionary<ChoiceActionItem, DashboardDefinition>();

        public void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
            extenders.Add<IModelDashboardModule, IModelDashboardModuleNavigation>();
        }

        protected override void OnDeactivated() {
            UnsubscribeFromEvents();
            base.OnDeactivated();
        }

        void SubscribeToEvents() {
            _navigationController = Frame.GetController<ShowNavigationItemController>();
            if (_navigationController != null)
                _navigationController.ItemsInitialized += _NavigationController_ItemsInitialized;
        }

        void UnsubscribeFromEvents() {
            if (_navigationController != null) {
                _navigationController.ItemsInitialized -= _NavigationController_ItemsInitialized;
                _navigationController = null;
            }
        }

        protected override void OnFrameAssigned() {
            UnsubscribeFromEvents();
            base.OnFrameAssigned();
            SubscribeToEvents();
        }

        void _NavigationController_ItemsInitialized(object sender, EventArgs e) {
            var view = Application.FindModelView(Application.FindListViewId(typeof(DashboardDefinition)));
            var dashboardOptions = ((IModelDashboardModuleNavigation) ( ((IModelApplicationDashboardModule) Application.Model)).DashboardModule);
            if (dashboardOptions.DashboardsInGroup) {
                ReloadDashboardActions();
                var actions = new List<ChoiceActionItem>();
                if (DashboardActions.Count > 0) {
                    var dashboardGroup = GetGroupFromActions(((ShowNavigationItemController)sender).ShowNavigationItemAction, dashboardOptions.DashboardGroupCaption);
                    if (dashboardGroup == null) {
                        dashboardGroup = new ChoiceActionItem(dashboardOptions.DashboardGroupCaption, null) {
                            ImageName = "BO_DashboardDefinition"
                        };
                        if (!string.IsNullOrEmpty(dashboardOptions.DashboardsParentItem)) {
                            var itemCollection = ((ShowNavigationItemController)sender).ShowNavigationItemAction.Items.GetItems<ChoiceActionItem>(item => item.Items);
                            var parent = itemCollection.FirstOrDefault(c => c.Id == dashboardOptions.DashboardsParentItem);
                            parent?.Items.Add(dashboardGroup);
                        }
                        else
                            ((ShowNavigationItemController)sender).ShowNavigationItemAction.Items.Add(dashboardGroup);
                    }
                    while (dashboardGroup.Items.Count != 0) {
                        ChoiceActionItem item = dashboardGroup.Items[0];
                        dashboardGroup.Items.Remove(item);
                        actions.Add(item);
                    }
                    foreach (ChoiceActionItem action in DashboardActions.Keys) {
                        action.Active["HasRights"] = HasRights(action, view);
                        actions.Add(action);
                    }
                    foreach (ChoiceActionItem action in actions.OrderBy(action => action.Model.Index))
                        dashboardGroup.Items.Add(action);

                }
            }
        }

        protected virtual bool HasRights(ChoiceActionItem item, IModelView view) {
            var data = (ViewShortcut)item.Data;
            if (view == null) {
                if (Application.GetPlatform() == Platform.Win) {
                    throw new ArgumentException($"Cannot find the '{data.ViewId}' view specified by the shortcut: {data}");
                }

                var webApi = Application.WhenWeb().Wait();
                webApi.Redirect(webApi.GetRequestUri().GetLeftPart(UriPartial.Authority));

            }
            var objectView = view as IModelObjectView;
            Type type = objectView?.ModelClass.TypeInfo.Type;
            if (type != null) {
                if (!string.IsNullOrEmpty(data.ObjectKey) && !data.ObjectKey.StartsWith("@")) {
                    try {
                        using IObjectSpace space = CreateObjectSpace();
                        object objectByKey = space.GetObjectByKey(type, space.GetObjectKey(type, data.ObjectKey));
                        return (DataManipulationRight.CanRead(type, null, objectByKey, null, space) &&
                                DataManipulationRight.CanNavigate(type, objectByKey, space));
                    }
                    catch {
                        return true;
                    }
                }
                return DataManipulationRight.CanNavigate(type, null, null);
            }
            return true;
        }

        protected virtual IObjectSpace CreateObjectSpace() {
            return Application.CreateObjectSpace(typeof(DashboardDefinition));
        }

        public virtual void UpdateNavigationImages() {
        }

        void ReloadDashboardActions() {
            DashboardActions.Clear();
            var objectSpace = Application.CreateObjectSpace(typeof(DashboardDefinition));
            var templates = objectSpace.GetObjects<DashboardDefinition>().Where(t => t.Active).OrderBy(i => i.Index);
            foreach (DashboardDefinition template in templates) {
                var action = new ChoiceActionItem(
                    template.Oid.ToString(),
                    template.Name,
                    new ViewShortcut(typeof(DashboardDefinition), template.Oid.ToString(), DashboardDefinition.DashboardViewerDetailView)) {
                    ImageName = "BO_DashboardDefinition"
                };
                action.Model.Index = template.Index;
                DashboardActions.Add(action, template);
            }
        }

        public void RecreateNavigationItems() {
            _navigationController.RecreateNavigationItems();
        }

        ChoiceActionItem GetGroupFromActions(SingleChoiceAction action, String name) {
            return action.Items.FirstOrDefault(item => item.Caption.Equals(name));
        }
    }

    public interface IModelDashboardModuleNavigation : IModelNode {
        [Category("Navigation")]
        [DefaultValue("Dashboards")]
        String DashboardGroupCaption { get; set; }

        [DefaultValue(true)]
        [Category("Navigation")]
        bool DashboardsInGroup { get; set; }

        [Category("Navigation")]
        String DashboardsParentItem { get; set; }
    }
}