<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="View.ascx.cs" Inherits="DNN.Modules.SecurityAnalyzer.View" EnableViewState="true"  %>
<%@ Import Namespace="DNN.Modules.SecurityAnalyzer.Components" %>
<%@ Import Namespace="DotNetNuke.Entities.Users" %>
<%@ Import Namespace="DotNetNuke.Services.Localization" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<div class="dnnForm" id="SecurityAnalyzer">
    <ul class="dnnAdminTabNav">
        <li>
            <a href="#auditChecks"><%= GetString("AuditChecks") %></a>
        </li>
        <li>
            <a href="#scannerChecks"><%= GetString("ScannerChecks") %></a>
        </li>
        <li>
            <a href="#superuserActivity"><%= GetString("SuperuserActivity") %></a>
        </li>
        <li>
            <a href="#modifiedFiles"><%= GetString("ModifiedFiles") %></a>
        </li>
        <li>
            <a href="#modifiedSettings"><%= GetString("ModifiedSettings") %></a>
        </li>
    </ul>
    <div id="auditChecks" class="dnnClear">

        <div class="dnnLeft"> 
            <strong>
                <asp:Label ID="lblAuditExplanation" runat="server" resourceKey="AuditExplanation"></asp:Label>
            </strong>
            <div>
                <asp:DataGrid id="dgResults" runat="server" AutoGenerateColumns="false" AllowPaging="false" visible="true" width="100%" GridLines="None" CssClass="dnnGrid">
                    <headerstyle CssClass="dnnGridHeader"/>
                    <itemstyle CssClass="dnnGridItem" horizontalalign="Left"/>
                    <alternatingitemstyle CssClass="dnnGridAltItem"/>
                    <edititemstyle/>
                    <selecteditemstyle/>
                    <footerstyle/>
                    <Columns>
                        <asp:TemplateColumn HeaderText="CheckPurpose">
                            <ItemTemplate>
                                <asp:label runat="server" Text="<%#  DisplayFriendlyName(((CheckResult) Container.DataItem).CheckNameText) %>" />
                            </ItemTemplate>
                        </asp:TemplateColumn>
                        <asp:TemplateColumn HeaderText="Severity" HeaderStyle-Width="50px" ItemStyle-HorizontalAlign="Center">
                            <ItemTemplate>
                                <asp:Image runat="server" 
                                    ImageUrl="<%# GetSeverityImageUrl((int) ((CheckResult) Container.DataItem).Severity) %>"
                                    Visible="<%# ((CheckResult) Container.DataItem).Severity != SeverityEnum.Unverified %>"></asp:Image>
                                <asp:LinkButton runat="server"
                                    CssClass="dnnPrimaryAction"
                                    ValidationGroup="AuditChecker"
                                    resourcekey="Check"
                                    OnClick="OnAuditCheck"
                                    CommandArgument="<%# ((CheckResult) Container.DataItem).CheckName %>"
                                    Visible="<%# ((CheckResult) Container.DataItem).Severity == SeverityEnum.Unverified %>"></asp:LinkButton>
                            </ItemTemplate>
                        </asp:TemplateColumn>
                        <asp:TemplateColumn HeaderText="Result">
                            <ItemTemplate>
                                <div class="foo" id="resultDiv" runat="server"><%# DisplayResult((CheckResult) Container.DataItem) %></div>
                            </ItemTemplate>
                        </asp:TemplateColumn>
                        <asp:TemplateColumn HeaderText="Notes">
                            <ItemTemplate>
                                <div class="foo" id="notesDiv" runat="server"><%# DisplayNotes(((CheckResult) Container.DataItem).Notes) %></div>
                            </ItemTemplate>
                        </asp:TemplateColumn>

                    </Columns>

                </asp:DataGrid>
            </div>

        </div>
    </div>
    <div id="scannerChecks" class="dnnClear">
        <asp:Panel ID="panelSearch" runat="server" CssClass="dnnFormItem dnnClear" Width="450px">
            <div class="dnnLeft">
                <asp:Label ID="lblScannerExplanation" runat="server" resourceKey="ScannerExplanation"></asp:Label>
                <div class="dnnFormItem">
                    <dnn:label id="plSearchTerm" controlname="txtSearchTerm" runat="server" CssClass="dnnFormRequired"/>
                    <asp:TextBox ID="txtSearchTerm" runat="server" MaxLength="256" Text="rootkit" ValidationGroup="ScannerChecks" />
                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" Display="Dynamic" 
                        EnableClientScript="true" ControlToValidate="txtSearchTerm" CssClass="dnnFormMessage dnnFormError"
                         resourcekey="SearchTermRequired" ValidationGroup="ScannerChecks"/>
                    <asp:LinkButton ID="cmdSearch" runat="server" CssClass="dnnPrimaryAction" resourcekey="cmdSearch" ValidationGroup="ScannerChecks"/>
                </div>
            </div>
        </asp:Panel>
        <br/>
        <hr/>
        <asp:Panel ID="pnlFileresults" runat="server" CssClass="dnnFormItem dnnClear" Width="450px" Visible="False">
            <strong>File Results</strong><br/>
            <asp:Label ID="lblfileresults" runat="server"></asp:Label>
        </asp:Panel><br/>
        <asp:Panel ID="pnlDatabaseresults" runat="server" CssClass="dnnFormItem dnnClear" Width="450px" Visible="False">
            <strong>Database Results</strong>
            <asp:Label ID="lbldatabaseresults" runat="server"></asp:Label>
        </asp:Panel>
    </div>

    <div id="superuserActivity" class="dnnClear">
        <strong>
            <asp:Label ID="lblSuperUserActivityExplaination" runat="server" resourceKey="SuperUserActivityExplaination"></asp:Label>
        </strong>
        <br/>
        <br/>
        <div>
            <asp:DataGrid id="dgUsers" runat="server" AutoGenerateColumns="false" AllowPaging="false" visible="true" width="100%" GridLines="None" CssClass="dnnGrid">
                <headerstyle CssClass="dnnGridHeader"/>
                <itemstyle CssClass="dnnGridItem" horizontalalign="Left"/>
                <alternatingitemstyle CssClass="dnnGridAltItem"/>
                <edititemstyle/>
                <selecteditemstyle/>
                <footerstyle/>
                <Columns>
                    <asp:BoundColumn datafield="UserName" headertext="Username"/>
                    <asp:BoundColumn datafield="FirstName" headertext="FirstName"/>
                    <asp:BoundColumn datafield="LastName" headertext="LastName"/>
                    <asp:BoundColumn datafield="DisplayName" headertext="DisplayName"/>
                    <asp:TemplateColumn HeaderText="Email">
                        <ItemTemplate>
                            <asp:Label ID="lblEmail" Runat="server" Text="<%# DisplayEmail(((UserInfo) Container.DataItem).Email) %>">
                            </asp:Label>
                        </ItemTemplate>
                    </asp:TemplateColumn>
                    <asp:TemplateColumn HeaderText="CreatedDate">
                        <ItemTemplate>
                            <asp:Label ID="lblCreateDate" Runat="server" Text="<%# DisplayDate(((UserInfo) Container.DataItem).Membership.CreatedDate) %>">
                            </asp:Label>
                        </ItemTemplate>
                    </asp:TemplateColumn>
                    <asp:TemplateColumn HeaderText="LastLogin">
                        <ItemTemplate>
                            <asp:Label ID="lblLastLogin" Runat="server" Text="<%# DisplayDate(((UserInfo) Container.DataItem).Membership.LastLoginDate) %>">
                            </asp:Label>
                        </ItemTemplate>
                    </asp:TemplateColumn>
                    <asp:TemplateColumn HeaderText="LastActivityDate">
                        <ItemTemplate>
                            <asp:Label ID="Label1" Runat="server" Text="<%# DisplayDate(((UserInfo) Container.DataItem).Membership.LastActivityDate) %>">
                            </asp:Label>
                        </ItemTemplate>
                    </asp:TemplateColumn>
                </Columns>
            </asp:DataGrid>
        </div>
    </div>
    <div id="modifiedFiles" class="dnnClear">
        <asp:Panel ID="panelModifiedFiles" runat="server" CssClass="dnnFormItem dnnClear" Width="450px">
            <div class="dnnLeft">
                <div class="dnnFormItem">
                    <asp:LinkButton ID="cmdModifiedFiles" runat="server" CssClass="dnnPrimaryAction" resourcekey="cmdModifiedFiles" ValidationGroup="ModifiedFiles"/>
                </div>
                <asp:Label ID="LabelCheckModifiedFiles" runat="server" resourceKey="ModifiedFilesLoadWarning"></asp:Label>
            </div>
        </asp:Panel>
        <br/>
         <strong>
            <asp:Label ID="lblModifiedFilesExplaination" runat="server" resourceKey="ModifiedFilesExplaination"></asp:Label>
        </strong>
        <br/>
        <br/>
        <div>
            <h2><%=GetString("HighRiskFiles") %></h2>
            <asp:DataGrid id="dgModifiedExecutableFiles" runat="server" AutoGenerateColumns="false" AllowPaging="false" visible="true" width="100%" GridLines="None" CssClass="dnnGrid">
                <headerstyle CssClass="dnnGridHeader"/>
                <itemstyle CssClass="dnnGridItem" horizontalalign="Left"/>
                <alternatingitemstyle CssClass="dnnGridAltItem"/>
                <edititemstyle/>
                <selecteditemstyle/>
                <footerstyle/>
                <Columns>
                    <asp:TemplateColumn HeaderText="FileName">
                        <ItemTemplate>
                            <span><%# GetFilePath(Eval("FullName").ToString()) %></span>
                        </ItemTemplate>
                    </asp:TemplateColumn>
                    <asp:TemplateColumn HeaderText="LastModifiedDate">
                        <ItemTemplate>
                            <span><%# DisplayDate(Convert.ToDateTime(Eval("LastWriteTime"))) %></span>
                        </ItemTemplate>
                    </asp:TemplateColumn>
                </Columns>
            </asp:DataGrid>
            <h2><%=GetString("LowRiskFiles") %></h2>
            <asp:DataGrid id="dgModifiedFiles" runat="server" AutoGenerateColumns="false" AllowPaging="false" visible="true" width="100%" GridLines="None" CssClass="dnnGrid">
                <headerstyle CssClass="dnnGridHeader"/>
                <itemstyle CssClass="dnnGridItem" horizontalalign="Left"/>
                <alternatingitemstyle CssClass="dnnGridAltItem"/>
                <edititemstyle/>
                <selecteditemstyle/>
                <footerstyle/>
                <Columns>
                    <asp:TemplateColumn HeaderText="FileName">
                        <ItemTemplate>
                            <span><%# GetFilePath(Eval("FullName").ToString()) %></span>
                        </ItemTemplate>
                    </asp:TemplateColumn>
                    <asp:TemplateColumn HeaderText="LastModifiedDate">
                        <ItemTemplate>
                            <span><%# DisplayDate(Convert.ToDateTime(Eval("LastWriteTime"))) %></span>
                        </ItemTemplate>
                    </asp:TemplateColumn>
                </Columns>
            </asp:DataGrid>
        </div>
    </div>
    <div id="modifiedSettings" class="dnnClear">
         <strong>
            <asp:Label ID="lblModifiedSettingsExplaination" runat="server" resourceKey="ModifiedSettingsExplaination"></asp:Label>
        </strong>
        <br/>
        <br/>
        <div>
            <h2><%=GetString("PortalSettings") %></h2>
            <asp:DataGrid id="dgPortalSettings" runat="server" AutoGenerateColumns="true" AllowPaging="false" visible="true" width="100%" GridLines="None" CssClass="dnnGrid">
                <headerstyle CssClass="dnnGridHeader"/>
                <itemstyle CssClass="dnnGridItem" horizontalalign="Left"/>
                <alternatingitemstyle CssClass="dnnGridAltItem"/>
                <edititemstyle/>
                <selecteditemstyle/>
                <footerstyle/>
            </asp:DataGrid>
        </div>
        <div>
            <h2><%=GetString("HostSettings") %></h2>
            <asp:DataGrid id="dgHostSettings" runat="server" AutoGenerateColumns="true" AllowPaging="false" visible="true" width="100%" GridLines="None" CssClass="dnnGrid">
                <headerstyle CssClass="dnnGridHeader"/>
                <itemstyle CssClass="dnnGridItem" horizontalalign="Left"/>
                <alternatingitemstyle CssClass="dnnGridAltItem"/>
                <edititemstyle/>
                <selecteditemstyle/>
                <footerstyle/>
            </asp:DataGrid>
        </div>
        <div>
            <h2><%=GetString("TabSettings") %></h2>
            <asp:DataGrid id="dgTabSettings" runat="server" AutoGenerateColumns="true" AllowPaging="false" visible="true" width="100%" GridLines="None" CssClass="dnnGrid">
                <headerstyle CssClass="dnnGridHeader"/>
                <itemstyle CssClass="dnnGridItem" horizontalalign="Left"/>
                <alternatingitemstyle CssClass="dnnGridAltItem"/>
                <edititemstyle/>
                <selecteditemstyle/>
                <footerstyle/>
            </asp:DataGrid>
        </div>
        <div>
            <h2><%=GetString("ModuleSettings") %></h2>
            <asp:DataGrid id="dgModuleSettings" runat="server" AutoGenerateColumns="true" AllowPaging="false" visible="true" width="100%" GridLines="None" CssClass="dnnGrid">
                <headerstyle CssClass="dnnGridHeader"/>
                <itemstyle CssClass="dnnGridItem" horizontalalign="Left"/>
                <alternatingitemstyle CssClass="dnnGridAltItem"/>
                <edititemstyle/>
                <selecteditemstyle/>
                <footerstyle/>
            </asp:DataGrid>
        </div>
    </div>
</div>
<asp:Panel runat="server" ID="panelResources" Visible="False">
    <script type="text/javascript" src="<%=Page.ResolveUrl("~/DesktopModules/DnnCorp/SecurityAnalyzer/Scripts/jquery-ui.min.js") %>"></script>
    <script type="text/javascript" src="<%=Page.ResolveUrl("~/DesktopModules/DnnCorp/SecurityAnalyzer/Scripts/dnnscripts.js?cdv=" + DateTime.Now.Ticks) %>"></script>
    <link rel="stylesheet" type="text/css" href="<%=Page.ResolveUrl("~/DesktopModules/DnnCorp/SecurityAnalyzer/Styles/dnnstyles.css?cdv=" + DateTime.Now.Ticks) %>"/>
</asp:Panel>
<script type="text/javascript">
    jQuery(function($) {
        $("#SecurityAnalyzer").dnnTabs();
    });
</script>
<script runat="server">
    protected string GetString(string key)
    {
        return Localization.GetString(key, "~/DesktopModules/DnnCorp/SecurityAnalyzer/App_LocalResources/View.ascx.resx");
    }
</script>