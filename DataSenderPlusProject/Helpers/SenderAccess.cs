using System;
using System.Data;
using System.Windows.Controls;

namespace DataSenderPlusProject.Helpers
{
    internal static class SenderAccess
    {
        public static bool[] ReadFlags(IDataReader reader)
        {
            var flags = new bool[5];
            while (reader.Read())
            {
                int typeId = Convert.ToInt32(reader["TypeID"]);
                if (typeId >= 1 && typeId <= flags.Length)
                    flags[typeId - 1] = reader["AccessFlag"] != DBNull.Value
                        && Convert.ToBoolean(reader["AccessFlag"]);
            }
            return flags;
        }

        public static void Apply(TabControl tabs, bool[] flags)
        {
            int firstEnabled = -1;
            for (int index = 0; index < flags.Length; index++)
            {
                ((TabItem)tabs.Items[index]).IsEnabled = flags[index];
                if (flags[index] && firstEnabled < 0) firstEnabled = index;
            }
            if (tabs.SelectedIndex < 0 || !((TabItem)tabs.SelectedItem).IsEnabled)
                tabs.SelectedIndex = firstEnabled;
        }
    }
}
