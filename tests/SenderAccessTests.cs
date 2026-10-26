using DataSenderPlusProject.Helpers;
using System;
using System.Data;
using System.Windows.Controls;

internal static class SenderAccessTests
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
    }

    [STAThread]
    private static void Main()
    {
        var table = new DataTable();
        table.Columns.Add("TypeID", typeof(int));
        table.Columns.Add("AccessFlag", typeof(bool));
        table.Rows.Add(5, true);
        table.Rows.Add(2, true);
        table.Rows.Add(1, false);
        table.Rows.Add(3, DBNull.Value);
        table.Rows.Add(99, true);
        bool[] flags;
        using (var reader = table.CreateDataReader()) flags = SenderAccess.ReadFlags(reader);
        Check(!flags[0] && flags[1] && !flags[2] && !flags[3] && flags[4], "Map by TypeID; false/null/missing disabled");
        var tabs = new TabControl();
        for (int i = 0; i < 5; i++) tabs.Items.Add(new TabItem { IsEnabled = false });
        SenderAccess.Apply(tabs, flags);
        Check(((TabItem)tabs.Items[1]).IsEnabled && ((TabItem)tabs.Items[4]).IsEnabled, "True overrides disabled XAML default");
        Check(tabs.SelectedIndex == 1, "Select first accessible tab");
        flags[1] = false;
        SenderAccess.Apply(tabs, flags);
        Check(tabs.SelectedIndex == 4, "Revoked selection moves to accessible tab");
        SenderAccess.Apply(tabs, new bool[5]);
        Check(tabs.SelectedIndex == -1, "No access leaves no selection");
        SenderAccess.Apply(tabs, new[] { true, true, true, true, true });
        for (int i = 0; i < 5; i++) Check(((TabItem)tabs.Items[i]).IsEnabled, "All true enables all tabs");
        Console.WriteLine("PASS: TypeID mapping, true/false/null/missing flags, disabled defaults, selection changes, and all tabs enabled.");
    }
}
