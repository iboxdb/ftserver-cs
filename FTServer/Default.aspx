<%@ Page Language="C#"  EnableSessionState="false" Async="true" AsyncTimeOut="30" %>
 
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html>
<head runat="server">
	<title>Default</title>
		<script runat="server">
		public void button1Clicked (object sender, EventArgs args)
		{
			FTServer.MainClass.test_main ();
			button1.Text = "";
			
		}
	</script>
</head>
<body>
	<form id="form1" runat="server">
		<asp:Button id="button1" runat="server" Text="Click me!" OnClick="button1Clicked" />
		cc
	</form>
</body>
</html>
