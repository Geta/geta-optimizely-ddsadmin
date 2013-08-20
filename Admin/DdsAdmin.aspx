<%@ Page AutoEventWireup="true" CodeBehind="DdsAdmin.aspx.cs" Inherits="Geta.DdsAdmin.Admin.DdsAdmin" Language="C#" %>
<%@ Import Namespace="Geta.DdsAdmin.Admin" %>

<!DOCTYPE html>
<html>
    <head runat="server">
        <meta http-equiv="Content-type" content="text/html; charset=UTF-8" />
        <title><%= CurrentStoreName %></title>
        <link type="text/css" rel='stylesheet' href='/content/themes/DDSAdmin/custom/minified.css' />
        <script src="//ajax.googleapis.com/ajax/libs/jquery/1.10.2/jquery.min.js" type="text/javascript"> </script>
        <script type="text/javascript">
            if (typeof jQuery == 'undefined') {
                document.write(unescape("%3Cscript src='/Scripts/jquery-1.10.2.min.js' type='text/javascript'%3E%3C/script%3E"));
            }
        </script>
        <script src="//ajax.aspnetcdn.com/ajax/jquery.ui/1.10.2/jquery-ui.min.js" type="text/javascript"> </script>
        <script type="text/javascript">
            if (typeof jQuery.ui == 'undefined') {
                document.write(unescape("%3Cscript src='/Scripts/jquery-ui-1.10.2.min.js' type='text/javascript'%3E%3C/script%3E"));
            }
        </script>
        <script src="//ajax.aspnetcdn.com/ajax/jquery.validate/1.11.1/jquery.validate.min.js" type="text/javascript"> </script>
        <script type="text/javascript">
            if (typeof jQuery.validate == 'undefined') {
                document.write(unescape("%3Cscript src='/Scripts/jquery.validate.min.js' type='text/javascript'%3E%3C/script%3E"));
            }
        </script>
        <script src="/Scripts/DataTables-1.9.4/media/js/jquery.dataTables.min.js" type="text/javascript"> </script>
        <script src="../Scripts/dataTables.jeditable.min.js" type="text/javascript"> </script>
    </head>
    <body>
        <asp:Panel runat="server" id="hdivNoStoreTypeSelected" Visible="False">
            <h3>No Store Type selected</h3>
        </asp:Panel>
        <asp:Panel runat="server" id="hdivStoreTypeDoesntExist" Visible="False">
            <h3>Selected Store Type does not exist</h3>
        </asp:Panel>

        <asp:Panel runat="server" id="hdivStoreTypeSelected" Visible="False">
            <form id="formAddNewRow" action="#" title="Add new <%= CurrentStoreName %>">
                <asp:repeater runat="server" ID="repForm" >
                    <HeaderTemplate>
                        <input type="text" name="form_Id" id="form_Id" rel="0" readonly="readonly" style="display: none" />
                    </HeaderTemplate>
                    <ItemTemplate>
                        <%# SetItem(Container) %>
                        <label for="form_<%# Item.PropertyName %>">
                            <%# Item.PropertyName %>
                            <input type="text" name="form_<%# Item.PropertyName %>" id="form_<%# Item.PropertyName %>" rel="<%# Container.ItemIndex + 1 %>" />
                        </label>
                    </ItemTemplate>
                </asp:repeater>
            </form>

            <h3><%= string.IsNullOrEmpty(CustomHeading)? string.Format("Selected Store Type: {0}", CurrentStoreName): CustomHeading %></h3>
            <%= CustomMessage %>

            <table class="display" id="storeItems">
                <thead>
                    <tr>
                        <th>Id</th>
                        <asp:Repeater runat="server" ID="repColumnsHeader">
                            <ItemTemplate>
                                <%# SetItem(Container) %>
                                <th><%# Item.PropertyName %></th>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td colspan="2" class="dataTables_empty">Loading data from server</td>
                    </tr>
                </tbody>
            </table>

            <script type="text/javascript" charset="utf-8">
                $(function() {
                    var storeParameter = "<%= Constants.StoreKey %>=<%= CurrentStoreName %>";
                    var dataTable = $('#storeItems').dataTable({
                        sDom: "Rlfrtip",
                        bJQueryUI: true,
                        bProcessing: true,
                        bServerSide: true,
                        sPaginationType: "full_numbers",
                        sAjaxSource: "Data.ashx?<%= Constants.OperationKey %>=read&" + storeParameter,
                        fnInitComplete: function(oSettings, json) {
                            initTooltip();
                        }
                    }).makeEditable({
                        sUpdateURL: "Data.ashx?<%= Constants.OperationKey %>=update&" + storeParameter,
                        sAddURL: "Data.ashx?<%= Constants.OperationKey %>=create&" + storeParameter,
                        sAddHttpMethod: "POST",
                        sDeleteHttpMethod: "POST",
                        sDeleteURL: "Data.ashx?<%= Constants.OperationKey %>=delete&" + storeParameter,
                        oAddNewRowButtonOptions: {
                            label: "Add...",
                            icons: { primary: 'ui-icon-plus' }
                        },
                        oDeleteRowButtonOptions: {
                            label: "Remove",
                            icons: { primary: 'ui-icon-trash' }
                        },
                        oAddNewRowFormOptions: {
                            title: 'Add a new row to <%= CurrentStoreName %>',
                            modal: false,
                            width: 450
                        },
                        sAddDeleteToolbarSelector: ".dataTables_length",
                        sAddNewRowFormId: "formAddNewRow",
                        aoColumns: [<%= GetColumnsScript() %>]
                    });

                    <%= GetInvisibleColumnsScript() %>;

                    /* The following will fix the toolbar not being encapsulated when using the DataTables with ColReorderWithResize */
                    var toolbar = dataTable.parent().find('.fg-toolbar');
                    if (!toolbar.length) {
                        var _toolbar = '<div class="fg-toolbar ui-toolbar ui-widget-header ui-corner-bl ui-corner-br ui-helper-clearfix" />';
                        dataTable.prevAll('div').wrapAll(_toolbar).end().nextAll('div').wrapAll(_toolbar);
                    }
                    
                    function initTooltip() {
                        var tooltip = $('<div />').css({
                            position: 'absolute',
                            display: 'none',
                            left: -9999,
                            top: -9999,
                            backgroundColor: '#fff',
                            border: '1px solid #000',
                            padding: 2,
                            'white-space' : 'nowrap'
                        }).appendTo('body');
                    
                        dataTable.find('tbody tr[id]').each(function() {
                            var row = $(this);
                            row.hover(function() {
                                tooltip.text(row.attr('id')).show();
                                row.mousemove(function(e) {
                                    tooltip.css({
                                        left: e.pageX + 16,
                                        top: e.pageY + 16
                                    });
                                });
                            }, function() {
                                tooltip.hide();
                            });
                        });
                    }
                });
            </script>
        </asp:Panel>
    </body>
</html>