using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace AutoHPMA.Helpers.DataHelper;

public class ExcelHelper
{
    private Dictionary<string, string> _qaDictionary = new Dictionary<string, string>();
    private static readonly object excelLock = new object();

    public ExcelHelper(string excelPath)
    {
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        LoadExcelData(excelPath);
    }

    private void LoadExcelData(string excelPath)
    {
        if (!File.Exists(excelPath))
            throw new FileNotFoundException($"Excel 文件未找到: {excelPath}");

        using (var package = new ExcelPackage(new FileInfo(excelPath)))
        {
            var worksheet = package.Workbook.Worksheets[0]; // 读取第一个表格
            int rowCount = worksheet.Dimension.Rows;

            for (int row = 1; row <= rowCount; row++)
            {
                string question = worksheet.Cells[row, 1].Text.Trim();
                string answer = worksheet.Cells[row, 2].Text.Trim();

                if (!string.IsNullOrEmpty(question) && !_qaDictionary.ContainsKey(question))
                {
                    _qaDictionary.Add(question, answer);
                }
            }
        }
    }

    public static string ReadExcelCell(string path, int row, int col)
    {
        lock (excelLock)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"文件不存在: {path}");
                return null;
            }

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(path)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                return worksheet.Cells[row, col].Text.Trim();
            }
        }
    }


    public string GetBestMatchingAnswer(string inputQuestion)
    {
        return TextMatchHelper.FindBestMatch(inputQuestion, _qaDictionary);
    }
}
