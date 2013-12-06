﻿<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PulsusLogViewer.aspx.cs" Inherits="Pulsus.SharePoint.Layouts.PulsusLogViewer" DynamicMasterPageFile="~masterurl/default.master" %>

<asp:Content ID="PageHead" ContentPlaceHolderID="PlaceHolderAdditionalPageHead" runat="server">
    <link rel="stylesheet" href="/Style Library/Pulsus/styles/pulsus.css" />
    <link rel="stylesheet" href="/Style Library/Pulsus/styles/kendo.common.min.css" />
    <link rel="stylesheet" href="/Style Library/Pulsus/styles/kendo.default.min.css" />
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="PlaceHolderMain" runat="server">
    
    <div class="pulsus-container">
        <div class="left">
            <div class="parameters clearfix">
                <label for="pulsus-period">Period</label>
                <input type="text" id="pulsus-period" />
                <label for="pulsus-search">Search</label>
                <input type="text" id="pulsus-search" />
                <div class="clearfix"></div>
            </div>
            <div id="pulsus-grid" class="grid"></div>
        </div>
        <div class="right">
            <div id="pulsus-details" class="details"></div>
        </div>
        <div class="clearfix"></div>
    </div>

    <script src="/Style Library/Pulsus/scripts/jquery.daterangepicker.js"></script>
    <script src="/Style Library/Pulsus/scripts/kendo.all.min.js"></script>
    <script src="/Style Library/Pulsus/scripts/pulsus.js"></script>
    <script id="level-template" type="text/x-kendo-template">
        #if(Level >= 70000){#
            <span class="label label-red">#=LevelString#</span>
        #}else if(Level >= 60000) {#
            <span class="label label-yellow">#=LevelString#</span>
        #}else if(Level >= 40000) {#
            <span class="label label-blue">#=LevelString#</span>
        #}else{#
            <span class="label label-green">#=LevelString#</span>
        #}#
    </script>
    <script id="tags-template" type="text/x-kendo-template">
        #var tags = Tags.split(' ');#
        #for(var i in tags){#
            #if (tags[i] != null && tags[i] != '') {#
                <span class="label label-default">#=tags[i]#</span>
            #}#
        #}#
    </script>
</asp:Content>

<asp:Content ID="PageTitle" ContentPlaceHolderID="PlaceHolderPageTitle" runat="server">
    Pulsus Log Viewer
</asp:Content>

<asp:Content ID="PageTitleInTitleArea" ContentPlaceHolderID="PlaceHolderPageTitleInTitleArea" runat="server">
</asp:Content>