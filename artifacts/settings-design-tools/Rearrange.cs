using System;
using System.Linq;
using System.Xml.Linq;
class Rearrange {
 static void Main(string[] args) {
  XNamespace w="http://schemas.microsoft.com/winfx/2006/xaml/presentation";
  XNamespace x="http://schemas.microsoft.com/winfx/2006/xaml";
  var doc=XDocument.Load(args[0],LoadOptions.PreserveWhitespace);
  var fields=doc.Descendants().Single(e=>(string)e.Attribute(x+"Name")=="SenderFields");
  var cards=fields.Elements(w+"Border").ToArray();
  fields.Name=w+"Grid";
  fields.AddFirst(new XElement(w+"Grid.ColumnDefinitions",new XElement(w+"ColumnDefinition",new XAttribute("Width","1.15*")),new XElement(w+"ColumnDefinition",new XAttribute("Width","14")),new XElement(w+"ColumnDefinition",new XAttribute("Width","*"))));
  cards[0].SetAttributeValue("Margin","0,0,0,0");
  cards[1].SetAttributeValue("Grid.Column","2"); cards[1].SetAttributeValue("Margin","0,0,0,0");
  foreach(var name in new[]{"ShowPassword","ShowFtpPassword"}) {
   var check=doc.Descendants().Single(e=>(string)e.Attribute(x+"Name")==name);
   var panel=check.Parent; panel.Name=w+"Grid";
   check.Attribute("DockPanel.Dock").Remove(); check.SetAttributeValue("Grid.Column","1");
   panel.AddFirst(new XElement(w+"Grid.ColumnDefinitions",new XElement(w+"ColumnDefinition"),new XElement(w+"ColumnDefinition",new XAttribute("Width","Auto"))));
  }
  doc.Save(args[0]);
 }
}