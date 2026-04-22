using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrialSequence", menuName = "Experiment/Trial Sequence")]
public class TrialSequenceAsset : ScriptableObject
{
    public List<TrialDefinition> trials = new List<TrialDefinition>();
}