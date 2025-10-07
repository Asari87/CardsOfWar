#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DeckGeneratorWindow : EditorWindow
{
    string deckName = "DefaultDeck";
    string outputRoot = "Assets/Scripts/Cards";
    bool overwriteExisting = false;
    bool aceHigh = true;
    bool zeroPadNumbers = true;

    string[] defaultRanks = new[] { "02","03","04","05","06","07","08","09","10","J","Q","K","A" };
    SerializedObject so;
    SerializedProperty ranksProp;

    [System.Serializable]
    class RankList : ScriptableObject { public string[] ranks; }
    RankList rankList;

    [MenuItem("Tools/Cards/Deck Generator")]
    static void Open() => GetWindow<DeckGeneratorWindow>("Deck Generator");

    void OnEnable()
    {
        if (rankList == null)
        {
            rankList = ScriptableObject.CreateInstance<RankList>();
            rankList.ranks = defaultRanks.ToArray();
        }

        so = new SerializedObject(rankList);
        ranksProp = so.FindProperty("ranks");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Deck Settings", EditorStyles.boldLabel);
        deckName = EditorGUILayout.TextField("Deck Name", deckName);
        outputRoot = EditorGUILayout.TextField("Output Root", outputRoot);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Rank Options", EditorStyles.boldLabel);
        zeroPadNumbers = EditorGUILayout.ToggleLeft("Zero-pad numeric ranks (02..10)", zeroPadNumbers);
        aceHigh = EditorGUILayout.ToggleLeft("Ace high (A = 14)", aceHigh);

        so.Update();
        EditorGUILayout.PropertyField(ranksProp, new GUIContent("Ranks"), true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
        overwriteExisting = EditorGUILayout.ToggleLeft("Overwrite existing assets", overwriteExisting);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Deck", GUILayout.Height(32)))
            Generate();
    }

    void Generate()
    {
        if (string.IsNullOrWhiteSpace(deckName))
        {
            EditorUtility.DisplayDialog("Deck Generator", "Please provide a Deck Name.", "OK");
            return;
        }

        string targetDir = Path.Combine(outputRoot, deckName).Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(targetDir))
        {
            EnsureFolder(outputRoot);
            EnsureFolder(Path.Combine(outputRoot, deckName).Replace("\\", "/"));
        }

        var ranks = rankList.ranks
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(NormalizeRankString)
            .ToList();

        if (ranks.Count == 0)
        {
            EditorUtility.DisplayDialog("Deck Generator", "Rank list is empty.", "OK");
            return;
        }

        int created = 0, skipped = 0, overwritten = 0;

        foreach (CardSO.CardSuit suit in System.Enum.GetValues(typeof(CardSO.CardSuit)))
        {
            if (suit == CardSO.CardSuit.Joker)
                continue;

            foreach (string rank in ranks)
            {
                string fileName = $"{suit}_{rank}.asset";
                string assetPath = $"{targetDir}/{fileName}";

                if (File.Exists(assetPath) && !overwriteExisting)
                {
                    skipped++;
                    continue;
                }

                var card = ScriptableObject.CreateInstance<CardSO>();
                card.suit = suit;
                card.rank = rank;
                card.value = ComputeValue(rank, aceHigh);

                if (File.Exists(assetPath) && overwriteExisting)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    AssetDatabase.SaveAssets();
                    overwritten++;
                }

                AssetDatabase.CreateAsset(card, assetPath);
                created++;
            }
        }

        // Add Jokers
        CreateJoker(targetDir, "Joker_Red", ref created, ref skipped, ref overwritten);
        CreateJoker(targetDir, "Joker_Black", ref created, ref skipped, ref overwritten);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Deck Generator",
            $"Deck generated successfully.\n\nCreated: {created}\nOverwritten: {overwritten}\nSkipped: {skipped}\n\nPath:\n{targetDir}",
            "OK"
        );
    }

    static void CreateJoker(string folder, string fileNameWithoutExt, ref int created, ref int skipped, ref int overwritten)
    {
        string assetPath = $"{folder}/{fileNameWithoutExt}.asset";

        if (File.Exists(assetPath))
        {
            AssetDatabase.DeleteAsset(assetPath);
            overwritten++;
        }

        var joker = ScriptableObject.CreateInstance<CardSO>();
        joker.suit = CardSO.CardSuit.Joker;
        joker.rank = fileNameWithoutExt.Replace("Joker_", "");
        joker.value = 999;

        AssetDatabase.CreateAsset(joker, assetPath);
        created++;
    }

    static string NormalizeRankString(string raw)
    {
        string s = raw.Trim().ToUpperInvariant();
        if (s is "JACK") s = "J";
        else if (s is "QUEEN") s = "Q";
        else if (s is "KING") s = "K";
        else if (s is "ACE") s = "A";
        return s;
    }

    static int ComputeValue(string rank, bool aceHigh)
    {
        switch (rank)
        {
            case "A": return aceHigh ? 14 : 1;
            case "K": return 13;
            case "Q": return 12;
            case "J": return 11;
            default:
                if (int.TryParse(rank, out int v)) return Mathf.Clamp(v, 2, 10);
                return 0;
        }
    }

    static void EnsureFolder(string path)
    {
        var parts = path.Split('/').Where(p => !string.IsNullOrEmpty(p)).ToArray();
        if (parts.Length == 0) return;

        for (int i = 1; i < parts.Length; i++)
        {
            string parent = string.Join("/", parts.Take(i));
            string child = string.Join("/", parts.Take(i + 1));
            if (!AssetDatabase.IsValidFolder(child))
                AssetDatabase.CreateFolder(parent, parts[i]);
        }
    }
}
#endif
