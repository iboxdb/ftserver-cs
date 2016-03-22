<%@ Page Language="C#"  EnableSessionState="false" Async="true" AsyncTimeOut="30" %>
<%@ import namespace="FTServer" %>
<%@ import namespace="System.Collections.Generic" %>

 <%
     List<String> discoveries = new List<String> ();
     
     using (var box = SDB.search_db.Cube()) {
     	foreach (String skw in SearchResource.engine.discover(box, 'a', 'z', 4,
				                                       '\u2E80', '\u9fa5', 1)) {
			discoveries.Add (skw);
		}
     }
 %>
<!DOCTYPE html>
<html>
    <head>        
        <meta http-equiv="content-type" content="text/html; charset=UTF-8" />
        <meta name="description" content="iBoxDB NoSQL Database Full Text Search Server FTS" />
        <title>Full Text Search Server</title>

        <link rel="stylesheet" type="text/css" href="css/semantic.min.css" /> 

        <style>
            td{ 
                white-space:nowrap; 
                overflow: hidden
            }

            body {
                margin-top: 100px;
                overflow:hidden;
            }
            body > .grid {

            }

            .column {
                max-width: 60%;
            }

        </style> 

    </head>
    <body> 
        <div class="ui middle aligned center aligned grid">
            <div class="column"  >

                <h2 class="ui teal header" > 
                    <i class="disk outline icon" style="font-size:82px"></i> Full Text Search Server
                </h2>
                <form class="ui large form"  action="s.aspx"  onsubmit="formsubmit()"  >
                    <div class="ui label input">
                        <div class="ui action input">
                            <input name="q"  value=""  onfocus="formfocus()" required />
                            <input id="btnsearch" type="submit"  class="ui teal right button big" 
                                   value="Search"    /> 
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

                <div class="ui message" style="text-align: left">
                    Input [KeyWord] to search,  input [URL] to index <br /> 
                    Input [delete URL] to delete.   <a  href="./">Refresh</a> 
                  
 					<br />Recent Searches:<br />
                    <%
                        foreach (String str in SearchResource.searchList) {

                    %> <a href="s.aspx?q=<%=str.Replace("#", "%23") %>"><%=str%></a>. &nbsp;  
                    <%
                        }
                    %>

                    <br>Recent Records:<br>
                    <%
                        foreach (String str in SearchResource.urlList) {
                    %>
                    <a href="<%=str%>" target="_blank" ><%=str%></a>. <br> 
                    <%
                        }
                    %>
                    
                    <br />Discoveries:&nbsp; 
   					<%
                        foreach (String str in discoveries) {

                    %> <a href="s.aspx?q=<%=str.Replace("#", "%23") %>"><%=str%></a>. &nbsp;  
                    <%
                        }
                    %>
                </div>

            </div>
        </div>

    </body>
</html>