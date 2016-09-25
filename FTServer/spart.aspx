<%@ Page Language="C#" Inherits="FTServer.spart" EnableSessionState="false" Async="true" AsyncTimeOut="30"%>
<%@ import namespace="FTServer" %>

<div id="ldiv<%= startId%>">
 <% 
                     foreach (var p in pages) {
                        String content = null;
                        if ( (pages.Count == 1 && isFirstLoad)|| p.keyWord == null) {
                        	content = p.description + "...";
                            content += p.content.ToString();
                        } else if (p.id != p.keyWord.ID) {
                            content = p.description;
                            if (content.Length < 20) {
                                content += p.getRandomContent();
                            }
                        } else {
                            content = SearchResource.engine.getDesc(p.content.ToString(), p.keyWord, 80); 
                            if (content.Length < 100) {
                                content += p.getRandomContent();
                            }
                            if (content.Length < 100) {
                                content += p.description;
                            }
                            if (content.Length > 200) {
                                content = content.Substring(0, 200) + "..";
                            }
                           
                        }
%>
<h3>
 <a class="stext" target="_blank"   href="<%= p.url%>" ><%= p.title%></a></h3> 
<span class="stext"> <%=content%> </span>
 <div class="gt">
        <%=p.url%>  
</div>                
                <% }%>
</div>
                
 <div class="ui teal message" id="s<%= startId%>">
    <%
       String tcontent = (DateTime.Now - begin).TotalSeconds + "s, "
                            + "MEM:" + (System.GC.GetTotalMemory(false) / 1024 / 1024) + "MB ";
    %>
    <%=name%>  TIME: <%= tcontent%>
    <a href="#btnsearch" ><b><%= pages.Count >= pageCount ? "HEAD" : "END"%></b></a>        
</div>            

<script>
    setTimeout(function () {
        highlight("ldiv<%= startId%>");
    <% if (pages.Count >= pageCount) {%>
        //startId is a big number, in javascript, have to write big number as a 'String'
        onscroll_loaddiv("s<%= startId%>", "<%= startId%>");
    <%}%>
    }, 100);
</script>      
      