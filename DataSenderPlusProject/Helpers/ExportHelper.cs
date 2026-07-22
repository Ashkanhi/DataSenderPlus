using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media;
using ClosedXML.Excel;
using System.Xml;

namespace DataSenderPlusProject.Helpers
{
    public class ExportHelper
    {
        public static void Export(
            DataTable table,
            int outputType,
            string fileName,
            string basePath)
        {

            string exportFolder =
                    Path.Combine(basePath, "ExportFile");

            Directory.CreateDirectory(exportFolder);
            switch (outputType)
            {
                // JSON
                case 1:

                    string json =
                        JsonConvert.SerializeObject(
                            table,
                            Newtonsoft.Json.Formatting.Indented);

                    File.WriteAllText(
                        Path.Combine(
                            exportFolder,
                            fileName + ".json"),
                        json);

                    break;
                case 2:

                    ExportCsv(
                        table,
                        Path.Combine(
                            exportFolder,
                            fileName + ".csv"));

                    break;
                case 3:

                    table.WriteXml(
                        Path.Combine(
                            exportFolder,
                            fileName + ".xml"),
                        XmlWriteMode.WriteSchema);

                    break;

                case 4:

                    ExportExcel(
                        table,
                        Path.Combine(
                            basePath,
                            "ExportFile",
                            fileName + ".xlsx"));

                    break;
                default:

                    throw new Exception("Output Type Not Supported.");
            }
        }

        private static void ExportCsv(
                DataTable table,
            string filePath)
        {
            StringBuilder csv = new StringBuilder();

            // ---------- عنوان ستون ها ----------
            for (int i = 0; i < table.Columns.Count; i++)
            {
                csv.Append(table.Columns[i].ColumnName);

                if (i < table.Columns.Count - 1)
                    csv.Append(",");
            }

            csv.AppendLine();

            // ---------- اطلاعات ----------
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    string value = row[i].ToString();

                    // اگر داخل متن کاما یا کوتیشن بود
                    value = value.Replace("\"", "\"\"");

                    csv.Append("\"" + value + "\"");

                    if (i < table.Columns.Count - 1)
                        csv.Append(",");
                }

                csv.AppendLine();
            }

            File.WriteAllText(
                filePath,
                csv.ToString(),
                Encoding.UTF8);
        }

        private static void ExportExcel(
            DataTable table,
            string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                // ایجاد شیت
                var worksheet =
                    workbook.Worksheets.Add("Data");

                // ---------- عنوان ستون ها ----------
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    worksheet.Cell(1, col + 1).Value =
                        table.Columns[col].ColumnName;

                    worksheet.Cell(1, col + 1).Style.Font.Bold = true;

                    worksheet.Cell(1, col + 1).Style.Fill.BackgroundColor =
                        XLColor.LightGray;
                }

                // ---------- اطلاعات ----------
                for (int row = 0; row < table.Rows.Count; row++)
                {
                    for (int col = 0; col < table.Columns.Count; col++)
                    {
                        worksheet.Cell(row + 2, col + 1).Value =
                            table.Rows[row][col]?.ToString();
                    }
                }

                // فعال کردن فیلتر
                worksheet.RangeUsed().SetAutoFilter();

                // تنظیم خودکار عرض ستون ها
                worksheet.Columns().AdjustToContents();

                // ذخیره فایل
                workbook.SaveAs(filePath);
            }
        }


        public static string GetFileExtension(int outputType)
        {
            switch (outputType)
            {
                case 1:
                    return ".json";

                case 2:
                    return ".csv";

                case 3:
                    return ".xml";

                case 4:
                    return ".xlsx";

                default:
                    return ".json";
            }
           
       }
     }
    }   
    