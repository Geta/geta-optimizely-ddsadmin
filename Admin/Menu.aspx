<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Menu.aspx.cs" Inherits="Geta.DdsAdmin.Admin.Menu" MasterPageFile="../Temp.Master" %>
<%@ Import Namespace="Geta.DdsAdmin.Admin" %>

<asp:Content ContentPlaceHolderID="HeaderContentRegion" runat="server">
    <base target="ep_main" />
</asp:Content>

<asp:Content ContentPlaceHolderID="FullRegion" runat="server">
    <div class="epi-buttonDefault" style="margin-left: 20px;">
        <span class="epi-cmsButton" >
            <a name="pLink" class="epi-cmsButton-tools epi-cmsButton-Edit"
               style="background-image: url(/App_Themes/Default/Images/Tools/SpriteTools.png); background-repeat: no-repeat; padding-left: 20px; line-height: 20px;"
               href="ExcludedStores.aspx">
                Exclude Stores
            </a>
        </span>
    </div>
    <div class="epi-buttonDefault" style="margin-left: 20px;">
        <span>
            <label for="txtListFilter">Filter: </label>
            <input type="text" id="txtListFilter" />
        </span>
    </div>
    <asp:Panel ID="tabView" runat="server" CssClass="epi-adminSidebar">
        <div class="epi-localNavigation">
            <ul>
                <li class="epi-navigation-standard epi-navigation-selected">
                    <asp:Repeater runat="server" ID="repStoreTypes">
                        <HeaderTemplate><ul id="storeList"></HeaderTemplate>
                        <ItemTemplate>
                            <li>
                                <a name="pLink" href="<%# string.Format("DdsAdmin.aspx?{0}={1}&{2}=0",
                                                   Constants.StoreKey,
                                                   Item.Name,
                                                   Constants.HiddenColumnsKey) %>">
                                    <%# string.Format("{0} ({1}, {2})", Item.Name, Item.Rows, Item.Columns.Count() + 1) %>
                                </a>
                            </li>
                        </ItemTemplate>
                        <FooterTemplate></ul></FooterTemplate>
                    </asp:Repeater>
                </li>
            </ul>
        </div>

        <script src="//ajax.googleapis.com/ajax/libs/jquery/1.10.2/jquery.min.js" type="text/javascript"> </script>
        <script type="text/javascript">
            if (typeof jQuery == 'undefined') {
                document.write(unescape("%3Cscript src='/Scripts/jquery-1.10.2.min.js' type='text/javascript'%3E%3C/script%3E"));
            }
        </script>
        <script src="../Scripts/listfilter.min.js" type="text/javascript"> </script>
        <script type="text/javascript">
            $(function() {
                $('#txtListFilter').listFilter({ listName: '#storeList' });
            });
        </script>
    </asp:Panel>
</asp:Content>