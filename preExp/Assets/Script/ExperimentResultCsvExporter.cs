using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class ExperimentResultCsvExporter : MonoBehaviour
{
    [Header("File")]
    public string filePrefix = "experiment_results";

    [Tooltip("留空时使用 Application.persistentDataPath")]
    public string customDirectory = "";

    public string SaveResults(List<TrialResult> results)
    {
        if (results == null || results.Count == 0)
        {
            Debug.LogWarning("[CSV] No results to save.");
            return string.Empty;
        }

        string fileName = $"{filePrefix}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";

        string directory = string.IsNullOrWhiteSpace(customDirectory)
            ? Application.persistentDataPath
            : customDirectory;

        // 如果目录不存在，就自动创建
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string path = Path.Combine(directory, fileName);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine(
            "trialId,blockId,trialType,targetDistanceMeters,actualDistanceMeters,subjectEstimateMeters," +
            "flashMode,panelCount,flashTriggerDistance,generatedPanelForwardDistances,generatedPanelLateralOffsets," +
            "startFeetX,startFeetY,startFeetZ,finishFeetX,finishFeetY,finishFeetZ,finishHeadX,finishHeadY,finishHeadZ," +
            "corridorForwardX,corridorForwardY,corridorForwardZ"
        );

        foreach (var r in results)
        {
            string panelForwards = JoinFloatArray(r.generatedPanelForwardDistances);
            string panelLaterals = JoinFloatArray(r.generatedPanelLateralOffsets);

            sb.AppendLine(
                $"{Escape(r.trialId)}," +
                $"{r.blockId}," +
                $"{r.trialType}," +
                $"{r.targetDistanceMeters}," +
                $"{r.actualDistanceMeters}," +
                $"{r.subjectEstimateMeters}," +
                $"{r.flashMode}," +
                $"{r.panelCount}," +
                $"{r.flashTriggerDistance}," +
                $"{Escape(panelForwards)}," +
                $"{Escape(panelLaterals)}," +
                $"{r.startFeetWorldPosition.x},{r.startFeetWorldPosition.y},{r.startFeetWorldPosition.z}," +
                $"{r.finishFeetWorldPosition.x},{r.finishFeetWorldPosition.y},{r.finishFeetWorldPosition.z}," +
                $"{r.finishHeadWorldPosition.x},{r.finishHeadWorldPosition.y},{r.finishHeadWorldPosition.z}," +
                $"{r.corridorForwardWorld.x},{r.corridorForwardWorld.y},{r.corridorForwardWorld.z}"
            );
        }

        File.WriteAllText(path, "\uFEFF" + sb.ToString(), Encoding.UTF8);

        Debug.Log($"[CSV] Saved results to: {path}");
        return path;
    }

    private string JoinFloatArray(float[] arr)
    {
        if (arr == null || arr.Length == 0)
            return string.Empty;

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < arr.Length; i++)
        {
            if (i > 0) sb.Append("|");
            sb.Append(arr[i].ToString("0.###"));
        }
        return sb.ToString();
    }

    private string Escape(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
        {
            s = s.Replace("\"", "\"\"");
            return $"\"{s}\"";
        }

        return s;
    }
}