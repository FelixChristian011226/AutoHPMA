using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoHPMA.Helpers;

public static class TextMatchHelper
{
    // 计算字符串相似度
    private static double CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0;

        int maxLen = Math.Max(source.Length, target.Length);
        int distance = LevenshteinDistance(source, target);

        return (1.0 - (double)distance / maxLen); // 归一化
    }

    // 使用 Levenshtein 距离计算两个字符串的编辑距离
    private static int LevenshteinDistance(string source, string target)
    {
        int[,] dp = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= target.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;
                dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
            }
        }
        return dp[source.Length, target.Length];
    }

    // 在问题列表中查找最匹配的问题，并返回答案
    public static string FindBestMatch(string input, Dictionary<string, string> qaDictionary)
    {
        string bestMatch = null;
        double highestScore = 0;

        foreach (var entry in qaDictionary)
        {
            double score = CalculateSimilarity(input, entry.Key);
            if (score > highestScore)
            {
                highestScore = score;
                bestMatch = entry.Value;
            }
        }

        return bestMatch ?? "未找到匹配项";
    }

    // 在选项中找到与答案最匹配的选项
    public static char FindBestOption(string answer, string a, string b, string c, string d)
    {
        Dictionary<char, string> options = new Dictionary<char, string>
        {
            { 'A', a },
            { 'B', b },
            { 'C', c },
            { 'D', d }
        };

        return options.OrderByDescending(opt => CalculateSimilarity(answer, opt.Value)).First().Key;
    }

    public static string FilterChineseAndPunctuation(string input)
    {
        // 仅保留中文字符和标点符号
        return Regex.Replace(input, "[a-zA-Z\\s]", "");
    }

}