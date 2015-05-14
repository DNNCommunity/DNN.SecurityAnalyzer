using System;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Definitions;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Security.Permissions;

namespace DNN.Modules.SecurityAnalyzer.Components
{
    public class FeatureController : IUpgradeable
    {
        public string UpgradeModule(string version)
        {
            switch (version)
            {
                case "01.00.00":
                    //Add Extensions Host Page
                    var moduleDefId = GetModuleDefinition("SecurityAnalyzer", "SecurityAnalyzer");
                    var auditPage = AddHostPage("Security Analyzer", "Audit site security for best practices.",
                        "~/images/icon_extensions_16px.gif", "~/images/icon_extensions_32px.gif", true);

                    var moduleid = AddModuleToPage(auditPage, moduleDefId, "Security Analyzer",
                        "~/images/icon_extensions_32px.gif");

                    break;
            }

            return String.Empty;
        }

        private static int AddModuleToPage(TabInfo page, int moduleDefId, string moduleTitle, string moduleIconFile)
        {
            //Call overload with InheritPermisions=True
            return AddModuleToPage(page, moduleDefId, moduleTitle, moduleIconFile, true);
        }

        public static int AddModuleToPage(TabInfo page, int moduleDefId, string moduleTitle, string moduleIconFile,
            bool inheritPermissions)
        {
            var moduleController = new ModuleController();
            ModuleInfo moduleInfo;
            var moduleId = Null.NullInteger;

            if ((page != null))
            {
                var isDuplicate = false;
                foreach (var kvp in moduleController.GetTabModules(page.TabID))
                {
                    moduleInfo = kvp.Value;
                    if (moduleInfo.ModuleDefID == moduleDefId)
                    {
                        isDuplicate = true;
                        moduleId = moduleInfo.ModuleID;
                    }
                }

                if (!isDuplicate)
                {
                    moduleInfo = new ModuleInfo
                    {
                        ModuleID = Null.NullInteger,
                        PortalID = page.PortalID,
                        TabID = page.TabID,
                        ModuleOrder = -1,
                        ModuleTitle = moduleTitle,
                        PaneName = Globals.glbDefaultPane,
                        ModuleDefID = moduleDefId,
                        CacheTime = 0,
                        IconFile = moduleIconFile,
                        AllTabs = false,
                        Visibility = VisibilityState.None,
                        InheritViewPermissions = inheritPermissions
                    };

                    try
                    {
                        moduleId = moduleController.AddModule(moduleInfo);
                    }
                    catch (Exception)
                    {
                        //DnnLog.Error(exc);
                    }
                }
            }

            return moduleId;
        }

        public static int AddModuleToPage(string tabPath, int portalId, int moduleDefId, string moduleTitle,
            string moduleIconFile, bool inheritPermissions)
        {
            var tabController = new TabController();
            var moduleId = Null.NullInteger;

            var tabID = TabController.GetTabByTabPath(portalId, tabPath, Null.NullString);
            if ((tabID != Null.NullInteger))
            {
                var tab = tabController.GetTab(tabID, portalId, true);
                if ((tab != null))
                {
                    moduleId = AddModuleToPage(tab, moduleDefId, moduleTitle, moduleIconFile, inheritPermissions);
                }
            }
            return moduleId;
        }

        private static int GetModuleDefinition(string desktopModuleName, string moduleDefinitionName)
        {
            // get desktop module
            var desktopModule = DesktopModuleController.GetDesktopModuleByModuleName(desktopModuleName, Null.NullInteger);
            if (desktopModule == null)
            {
                return -1;
            }

            // get module definition
            var objModuleDefinition = ModuleDefinitionController.GetModuleDefinitionByFriendlyName(
                moduleDefinitionName, desktopModule.DesktopModuleID);
            if (objModuleDefinition == null)
            {
                return -1;
            }


            return objModuleDefinition.ModuleDefID;
        }

        public static TabInfo AddHostPage(string tabName, string description, string tabIconFile,
            string tabIconFileLarge, bool isVisible)
        {
            var tabController = new TabController();
            var hostPage = tabController.GetTabByName("Host", Null.NullInteger);

            if ((hostPage != null))
            {
                return AddPage(hostPage, tabName, description, tabIconFile, tabIconFileLarge, isVisible,
                    new TabPermissionCollection(), true);
            }
            return null;
        }

        private static TabInfo AddPage(TabInfo parentTab, string tabName, string description, string tabIconFile,
            string tabIconFileLarge, bool isVisible, TabPermissionCollection permissions, bool isAdmin)
        {
            var parentId = Null.NullInteger;
            var portalId = Null.NullInteger;

            if ((parentTab != null))
            {
                parentId = parentTab.TabID;
                portalId = parentTab.PortalID;
            }


            return AddPage(portalId, parentId, tabName, description, tabIconFile, tabIconFileLarge, isVisible,
                permissions, isAdmin);
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        ///     AddPage adds a Tab Page
        /// </summary>
        /// <param name="portalId">The Id of the Portal</param>
        /// <param name="parentId">The Id of the Parent Tab</param>
        /// <param name="tabName">The Name to give this new Tab</param>
        /// <param name="description">Description.</param>
        /// <param name="tabIconFile">The Icon for this new Tab</param>
        /// <param name="tabIconFileLarge">The large Icon for this new Tab</param>
        /// <param name="isVisible">A flag indicating whether the tab is visible</param>
        /// <param name="permissions">Page Permissions Collection for this page</param>
        /// <param name="isAdmin">Is and admin page</param>
        private static TabInfo AddPage(int portalId, int parentId, string tabName, string description,
            string tabIconFile, string tabIconFileLarge, bool isVisible, TabPermissionCollection permissions,
            bool isAdmin)
        {
            var tabController = new TabController();

            var tab = tabController.GetTabByName(tabName, portalId, parentId);

            if (tab == null || tab.ParentId != parentId)
            {
                tab = new TabInfo
                {
                    TabID = Null.NullInteger,
                    PortalID = portalId,
                    TabName = tabName,
                    Title = "",
                    Description = description,
                    KeyWords = "",
                    IsVisible = isVisible,
                    DisableLink = false,
                    ParentId = parentId,
                    IconFile = tabIconFile,
                    IconFileLarge = tabIconFileLarge,
                    IsDeleted = false
                };
                tab.TabID = tabController.AddTab(tab, !isAdmin);

                if (((permissions != null)))
                {
                    foreach (TabPermissionInfo tabPermission in permissions)
                    {
                        tab.TabPermissions.Add(tabPermission, true);
                    }
                    TabPermissionController.SaveTabPermissions(tab);
                }
            }

            return tab;
        }
    }
}