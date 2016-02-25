<%@ Page Language="C#" Inherits="FTServer.s"  EnableSessionState="false" Async="true" AsyncTimeOut="30"%>
<%@ import namespace="FTServer" %>
<!DOCTYPE html>
<html>
    <head>        
        <meta http-equiv="content-type" content="text/html; charset=UTF-8">
        <meta name="description" content="<%=name%> what is? iBoxDB NoSQL Database Full Text Search">
        <title><%=name%>, what is? iBoxDB Full Text Search</title>

        <link rel="stylesheet" type="text/css" href="css/semantic.min.css"> 

        <style>
            body {
                margin-top: 10px;
                margin-left: 10px;
                font-weight:lighter;
                overflow-x: hidden;
            }
            .stext{

            }
            .redtext{
                color: red;
            }
        </style> 
        <script>
            function hightlight() {
                var txt = document.title.substr(0, document.title.indexOf(','));

                var ts = document.getElementsByClassName("stext");

                var kws = txt.split(' ');
                for (var i = 0; i < kws.length; i++) {
                    var kw = String(kws[i]).trim();
                    if (kw.length < 1) {
                        continue;
                    }
                    var fontText = "<font class='redtext'>";
                    if (fontText.indexOf(kw) > -1) {
                        continue;
                    }
                    if ("</font>".indexOf(kw) > -1) {
                        continue;
                    }
                    for (var j = 0; j < ts.length; j++) {
                        var html = ts[j].innerHTML;
                        ts[j].innerHTML =
                                html.replace(new RegExp(kw, 'gi'),
                                        fontText + kw + "</font>");
                    }
                }
            }
        </script>
    </head>
    <body onload="hightlight()"> 
        <div class="ui left aligned grid">
            <div class="column"  style="max-width: 600px;"> 
                <form class="ui large form"  action="s.aspx" onsubmit="formsubmit()">
                    <div class="ui label input">

                        <div class="ui action input">
                            <a href="./"><i class="teal disk outline icon" style="font-size:42px"></i> </a>
                            <input name="q"  value="<%=name%>" required onfocus="formfocus()" />
                            <input id="btnsearch" type="submit"  class="ui teal right button" value="Search" /> 
                        </div>
                    </div>
                </form> 
                <script>
                    function formsubmit() {
                        btnsearch.disabled = "disabled";
                    }
                    function formfocus() {
                        btnsearch.disabled = undefined;
                    }
                </script>
            </div>
        </div>

        <div class="ui grid">
            <div class="ten wide column" style="max-width: 600px;">
                <% foreach (var p in pages) {
                        String content = null;
                        if (pages.Count == 1 || p.keyWord == null) {
                            content = p.content.ToString();
                        } else if (p.id != p.keyWord.ID) {
                            content = p.description;
                        } else {
                            content = SearchResource.engine.getDesc(p.content.ToString(), p.keyWord, 80); 
                            if (content.Length < 100) {
                                content += p.getRandomContent();
                            }
                            if (content.Length < 100) {
                                content += p.description;
                            }
                            if (content.Length > 200) {
                                content = content.Substring(0, 200);
                            }
                        }
                %>
                <h3>
                    <a class="stext" target="_blank"   href="<%=p.url%>" ><%= p.title%></a></h3> 
                <span class="stext"> <%=content%> </span>
                <% }%>


            </div>
            <div class="six wide column" style="max-width: 200px;">
                <%
                    String tcontent = (DateTime.Now - begin).TotalSeconds + "s, "
                            + "MEM:" + (System.GC.GetTotalMemory(false) / 1024 / 1024) + "MB ";
                %>
                <div class="ui segment">
                    <h4>Time</h4> 
                    <%= tcontent%>
                </div>


            </div>
        </div>

    </body>
</html>