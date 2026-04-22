using System;
using UnityEngine;

[Serializable]
public class BilingualText
{
    [TextArea(2, 5)]
    public string japanese;

    [TextArea(2, 5)]
    public string english;

    public string Build()
    {
        bool hasJa = !string.IsNullOrWhiteSpace(japanese);
        bool hasEn = !string.IsNullOrWhiteSpace(english);

        if (hasJa && hasEn) return japanese + "\n\n" + english;
        if (hasJa) return japanese;
        if (hasEn) return english;
        return string.Empty;
    }
}