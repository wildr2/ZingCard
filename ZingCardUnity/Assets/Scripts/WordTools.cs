using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class WordTools : MonoBehaviour
{
    public bool extract_word_rels = false;

    private void Start()
    {
        //ExtractWordNetWords(
        //    "Assets/Texts/Wordnet/index.adj",
        //    "Assets/Texts/Wordnet/adjectives.txt",
        //    "Assets/Texts/20k.txt");
        //CheapPluralizeWords("Assets/Texts/3/nouns.txt", "Assets/Texts/3/cheap_plurals.txt");
        //TestCFG();
        //StartCoroutine(AssistedWordsGenRoutine("Assets/Texts/1/nouns.txt", "Assets/Texts/3/nouns.txt"));

        if (extract_word_rels)
        {
            StartCoroutine(ExtractWordRelations(
            "Assets/Texts/Fairy Tales of Hans Christian Andersen.txt",      // corpus
            "Assets/Texts/common_nouns.txt",        // head words
            "Assets/Texts/adjectives.txt",        // leaf words
            "Assets/Texts/rel_noun_adj_3.txt",      // relations
            false));                                // head leaf or leaf head  in text
        }
        else
        {
            GenPhrasesAndTargetWords(32, true);
        }
    }

    public static Pair<string[], string[]> GenPhrasesAndTargetWords(int n, bool log=false)
    {
        string[] phrases = new string[n];
        string[] target_words = new string[n];

        Dictionary<string, HashSet<string>> nouns_r_adjs = ReadWordRelations("Assets/Texts/rel_noun_adj_3.txt");
        HashSet<string> used_words = new HashSet<string>();

        for (int i = 0; i < n; ++i)
        {
            // Generate phrase
            string np1 = GenerateNounPhrase(nouns_r_adjs, used_words);
            string np2 = GenerateNounPhrase(nouns_r_adjs, used_words);
            phrases[i] += np1 + " and " + np2;


            // Pick target word
            string[] np1_words = np1.Split(new char[] { ' ' });
            string[] np2_words = np2.Split(new char[] { ' ' });
            string[] words = phrases[i].Split(new char[] { ' ' });

            float[] target_i_weights = new float[words.Length];
            for (int j = 0; j < target_i_weights.Length; ++j) target_i_weights[j] = 0.5f;

            target_i_weights[np1_words.Length] = 0; // and
            target_i_weights[np1_words.Length - 1] = 1; // np1 noun
            target_i_weights[words.Length - 1] = 1; // np2 noun

            target_words[i] = words[Tools.WeightedChoice(target_i_weights)];


            // Log
            if (log)
            {
                string s = "";
                foreach (string word in words)
                    s += (word == target_words[i] ? Tools.ColorRichTxt(word, Color.blue) : word) + ' ';
                Tools.Log(s);
            }
        }

        return new Pair<string[], string[]>(phrases, target_words);
    }
    public static string GenerateNounPhrase(Dictionary<string, HashSet<string>> nouns_r_adjs, HashSet<string> used_words)
    {
        string s = "";

        Pair<string, List<string>> noun_adjs = RandomNounAdjs(nouns_r_adjs, used_words);

        // Choose Adjectives
        int a = noun_adjs.Second.Count;
        int adj_n = Tools.WeightedChoice(new float[] { a > 0 ? 0 : 1, a > 0 ? 1 : 0, a > 1 ? 0.5f : 0 });

        if (adj_n > 0)
        {
            // First adj
            int r = Random.Range(0, noun_adjs.Second.Count);
            s += noun_adjs.Second[r] + ' ';
            used_words.Add(noun_adjs.Second[r]);

            if (adj_n > 1)
            {
                // Second adj (non duplicate)
                r = (r + Random.Range(1, noun_adjs.Second.Count - 1)) % noun_adjs.Second.Count;
                s += noun_adjs.Second[r] + ' ';
                used_words.Add(noun_adjs.Second[r]);
            }
        }

        // Add noun to beginning and pluralize
        s += Pluralize(noun_adjs.First);
        used_words.Add(noun_adjs.First);

        return s;
    }
    public static Pair<string, List<string>> RandomNounAdjs(Dictionary<string, HashSet<string>> nouns_r_adjs, HashSet<string> forbidden_words)
    {
        // Pick noun
        int i = Random.Range(0, nouns_r_adjs.Count);
        int start_i = i;
        string noun;
        do
        {
            i = (i + 1) % nouns_r_adjs.Count;
            if (i == start_i) Debug.LogError("Not enough words");
            noun = nouns_r_adjs.ElementAt(i).Key;

        } while (forbidden_words != null && forbidden_words.Contains(noun));

        // Find non forbidden adjectives
        List<string> adjs = new List<string>();
        foreach (string adj in nouns_r_adjs.ElementAt(i).Value)
        {
            if (forbidden_words == null || !forbidden_words.Contains(adj))
                adjs.Add(adj);
        }

        // Return
        Pair<string, List<string>> pair = new Pair<string, List<string>>();
        pair.First = noun;
        pair.Second = adjs;
        return pair;
    }


    public static string RandomWord(string[] word_lines, string[] forbidden = null)
    {
        int attempts = 0;
        int i = UnityEngine.Random.Range(0, word_lines.Length);
        string word;
        do
        {
            if (attempts > 10)
            {
                Debug.LogError("Not enough words");
                return "";
            }

            i = (i + 1) % word_lines.Length;
            word = word_lines[i].Trim(new char[] { ' ', '\n', '\r' });
            ++attempts;
        } while (forbidden != null && forbidden.Contains(word));

        return word;
    }
    public static string RandomWord(string[] word_lines, HashSet<string> forbidden)
    {
        int attempts = 0;
        int i = UnityEngine.Random.Range(0, word_lines.Length);
        string word;
        do
        {
            if (attempts > 100)
            {
                Debug.LogError("Not enough words");
                return "";
            }

            i = (i + 1) % word_lines.Length;
            word = word_lines[i].Trim(new char[] { ' ', '\n', '\r' });
            ++attempts;
        } while (forbidden != null && forbidden.Contains(word));

        return word;
    }

    public static Dictionary<string, HashSet<string>> ReadWordRelations(string data_file)
    {
        Dictionary<string, HashSet<string>> relations = new Dictionary<string, HashSet<string>>();

        StreamReader reader = new StreamReader(data_file);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            string[] s = line.Split(new char[] { ':' });
            string head = s[0].Trim();
            HashSet<string> leaves = new HashSet<string>();
            foreach (string word in s[1].Split(new char[] { ' ' }))
            {
                if (word == "") continue;
                leaves.Add(word);
            }
                

            relations.Add(head, leaves);
        }
        reader.Close();

        return relations;
    }
    public IEnumerator ExtractWordRelations(string corpus_file, string headwords_file, string leafwords_file, string relations_file, bool head_then_leaf = true)
    {

        // Read type 1 and 2 word data
        Tools.Log("Reading type 1 and 2 word data");
        StreamReader head_stream = new StreamReader(headwords_file);
        StreamReader leaf_stream = new StreamReader(leafwords_file);

        HashSet<string> head_words = new HashSet<string>();
        HashSet<string> leaf_words = new HashSet<string>();

        string word;
        while ((word = head_stream.ReadLine()) != null) head_words.Add(word);
        while ((word = leaf_stream.ReadLine()) != null) leaf_words.Add(word);

        head_stream.Close();
        leaf_stream.Close();


        // Read existing relations
        yield return null;
        Tools.Log("Reading existing relations");
        Dictionary<string, HashSet<string>> relations = ReadWordRelations(relations_file);

        // Reading and parsing corpus
        yield return null;
        Tools.Log("Reading and parsing corpus");
        string corpus = File.ReadAllText(corpus_file);
        string[] words = corpus.Split(new char[] { ' ' });

        string[] dets = new string[] { "a", "an" };//, "the", "one", "his", "her", "my", "our", "their", "your", "its" };

        int new_heads = 0;
        int new_leaves = 0;

        for (int i = 0; i < words.Length-2; ++i)
        {
            // Check for determinant
            if (dets.Contains(words[i]))
            {
                // Check next two words for head and leaf types
                string first = words[i + 1];
                string second = words[i + 2];

                // First word is head
                if (head_then_leaf && head_words.Contains(first) && leaf_words.Contains(second))
                {
                    HashSet<string> leaf_list;
                    if (relations.TryGetValue(first, out leaf_list))
                    {
                        // Entry exists for this head
                        if (leaf_list.Add(second)) ++new_leaves;
                    }
                    else
                    {
                        // New head entry
                        relations.Add(first, new HashSet<string>() { second });
                        ++new_heads;
                        ++new_leaves;
                    }
                    ++i;
                }

                // Second word is head
                else if (!head_then_leaf && leaf_words.Contains(first) && head_words.Contains(second))
                {
                    HashSet<string> leaf_list;
                    if (relations.TryGetValue(second, out leaf_list))
                    {
                        // Entry exists for this head
                        if (leaf_list.Add(first)) ++new_leaves;
                    }
                    else
                    {
                        // New head entry
                        relations.Add(second, new HashSet<string>() { first });
                        ++new_heads;
                        ++new_leaves;
                    }
                    ++i;
                }
            }
        }

        // Write relations
        yield return null;
        Tools.Log("Writing relations");
        StreamWriter writer = new StreamWriter(relations_file, false);
        foreach (KeyValuePair<string, HashSet<string>> relation in relations)
        {
            string s = relation.Key + " : ";
            foreach (string leaf in relation.Value) s += leaf + ' ';
            writer.WriteLine(s);
        }
        writer.Close();

        yield return null;
        Tools.Log("Done extracting relations - " + new_heads + " new heads, " + new_leaves + " new leaves");
    }
    public void TestGenPhrases(string nouns_r_adjs_file, string adjs_r_advs_file)
    {
        Dictionary<string, HashSet<string>> nouns_r_adjs = ReadWordRelations(nouns_r_adjs_file);
        Dictionary<string, HashSet<string>> adjs_r_advs = ReadWordRelations(adjs_r_advs_file);

        CFG cfg = new CFG();
        cfg.AddRule("S", "N and N");
        cfg.AddRule("N", "adj p");

        for (int i = 0; i < 10; ++i)
        {
            string format = cfg.Generate();
            string[] symbols = format.Split(new char[] { ' ' });
            string[] words = new string[symbols.Length];

            int j = symbols.Length - 1;
            HashSet<string> noun_adjs = new HashSet<string>();
            HashSet<string> adj_advs = new HashSet<string>();
            while (j >= 0)
            {
                if (symbols[j] == "p")
                {
                    int r = Random.Range(0, nouns_r_adjs.Count);
                    words[j] = nouns_r_adjs.ElementAt(r).Key + 's';
                    noun_adjs = nouns_r_adjs.ElementAt(r).Value;
                }
                else if (symbols[j] == "adj")
                {
                    if (noun_adjs.Count == 0) words[j] = "";
                    else
                    {
                        int r = Random.Range(0, noun_adjs.Count);
                        words[j] = noun_adjs.ElementAt(r);
                        //adj_advs = adjs_r_advs[words[j]];
                    }
                }
                else if (symbols[j] == "adv")
                {
                    if (adj_advs.Count == 0) words[j] = "";
                    else
                    {
                        int r = Random.Range(0, adj_advs.Count);
                        words[j] = adj_advs.ElementAt(r);
                    }
                }
                else
                {
                    words[j] = symbols[j];
                }

                --j;
            }


            string phrase = "";
            foreach (string word in words)
                if (word != "") phrase += word + ' ';
            Tools.Log(phrase + "(" + format + ")");
        }
    }

    public static void ExtractWordNetWords(string index_file, string out_file, string allowed_file)
    {
        // Put allowed words in dictionary
        HashSet<string> allowed = new HashSet<string>();
        string[] lines = File.ReadAllLines(allowed_file);
        foreach (string line in lines) allowed.Add(line.Trim());

        // Read wordnet words
        StreamWriter writer = new StreamWriter(out_file, false);
        lines = File.ReadAllLines(index_file);
        char[] rejectors = new char[] { '_', ',', '.' };

        foreach (string line in lines)
        {
            if (line.Length == 0 || line[0] == ' ') continue;

            string word = "";
            for (int i = 0; i < line.Length; ++i)
            {
                if (line[i] == ' ')
                {
                    if (allowed.Contains(word))
                        writer.WriteLine(word);
                    break;
                }
                if (rejectors.Contains(line[i])) break;
                word += line[i];
            }
        }

        writer.Close();

        Tools.Log("Done");
    }
    public static void PluralizeWords(string in_file, string out_file)
    {
        StreamWriter writer = new StreamWriter(out_file, false);
        string[] lines = File.ReadAllLines(in_file);

        foreach (string line in lines)
        {
            if (line != "") writer.WriteLine(Pluralize(line.Trim()));
        }
        writer.Close();
        Tools.Log("Done");
    }
    public static void TestCFG()
    {
        CFG cfg = new CFG();
        //cfg.AddRule("S", "NP VP");
        //cfg.AddRule("NP", "Det N");
        //cfg.AddRule("VP", "V NP");
        //cfg.AddRule("Det", "the");
        //cfg.AddRule("N", "Adj n | N n | n");
        //cfg.AddRule("V", "adv v | v adv | v");
        //cfg.AddRule("Adj", "Adj adj | adj");
        //cfg.AddRule("Adv", "Adv adv | adv");

        //cfg.AddRule("S", "adj adj p v adv");
        cfg.AddRule("S", "N V");
        cfg.AddRule("N", "Adj p | Adj n p | n p | p");
        cfg.AddRule("V", "adv v | adv v N | v adv | v N | v");
        cfg.AddRule("Adj", "Adj adj | adj");

        string[] nouns = File.ReadAllLines("Assets/Texts/Wordnet/nouns.txt");
        string[] verbs = File.ReadAllLines("Assets/Texts/Wordnet/verbs.txt");
        string[] adjectives = File.ReadAllLines("Assets/Texts/Wordnet/adjectives.txt");
        string[] adverbs = File.ReadAllLines("Assets/Texts/Wordnet/adverbs.txt");


        for (int i = 0; i < 20; ++i)
        {
            string format = cfg.Generate("S", 0.75f);
            string phrase = "";
            foreach (string symbol in format.Split(new char[] { ' ' }))
            {
                switch (symbol)
                {
                    case "n": phrase += RandomWord(nouns); break;
                    case "p": phrase += RandomWord(nouns) + 's'; break;
                    case "v": phrase += RandomWord(verbs); break;
                    case "adj": phrase += RandomWord(adjectives); break;
                    case "adv": phrase += RandomWord(adverbs); break;
                    default: Tools.Log("unrecognized symbol"); break;
                }
                phrase += ' ';
            }

            Tools.Log(phrase + "  (" + format + ")");
        }
    }
    private IEnumerator AssistedWordsGenRoutine(string in_file, string out_file)
    {
        string[] lines = File.ReadAllLines(in_file);
        HashSet<string> chosen = new HashSet<string>();
        bool done = false;
        while (!done)
        {
            string word = RandomWord(lines, chosen);
            Tools.Log(word);
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow)) break; // Reject
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    // Choose
                    chosen.Add(word);
                    Tools.Log(word + "  (" + chosen.Count + ")", Color.blue);
                    break;
                }
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    // End
                    done = true;
                    break;
                }
                yield return null;
            }
            yield return null;
        }

        // Write
        StreamWriter writer = new StreamWriter(out_file, true);
        foreach (string word in chosen)
            writer.WriteLine(word);
        //writer.Write('\n');
        writer.Close();
        Tools.Log("Done (" + chosen.Count + ")");
    }

    public static string Pluralize(string word)
    {
        if (word.EndsWith("x") || word.EndsWith("ch") || word.EndsWith("sh") || word.EndsWith("ss"))
            return word + "es";

        if (word.EndsWith("y") && word.Length > 1 && !"aeiouAEIOU".Contains(word[word.Length - 2]))
            return word.Remove(word.Length - 1) + "ies";

        return word + 's';
    }
}

//public class CFG
//{
//    private Dictionary<string, string[]> rules = new Dictionary<string, string[]>();
//    Dictionary<string, int> prod_counts = new Dictionary<string, int>();

//    public void AddRule(string lhs, string rhs)
//    {
//        string[] prods = rhs.Split(new char[] { '|' });
//        for (int i = 0; i < prods.Length; ++i)
//        {
//            prods[i] = prods[i].Trim();
//            prod_counts[prods[i]] = 0;
//        }
//        rules[lhs] = prods;
//    }
//    public string Generate(string format="S", float recurse_factor=0.5f)
//    {
//        //foreach (KeyValuePair<string, int> count in prod_counts) Tools.Log(count.Key + ": " + count.Value);
//        return _Generate(format, recurse_factor);
//    }

//    private string _Generate(string prods, float factor)
//    {
//        string s = "";

//        foreach (string prod in prods.Split(new char[] { ' ' }))
//        {
//            string[] choices;

//            if (rules.TryGetValue(prod, out choices)) // non terminal
//            {
//                float[] choice_weights = new float[choices.Length];
//                for (int i = 0; i < choices.Length; ++i)
//                    choice_weights[i] = Mathf.Pow(factor, prod_counts[choices[i]]);

//                int choice_i = Tools.WeightedChoice(choice_weights);

//                prod_counts[choices[choice_i]]++;
//                s += _Generate(choices[choice_i], factor);
//                prod_counts[choices[choice_i]]--; // undo change to production count dictionary (reference type)
//            }
//            else // terminal
//            {
//                s += prod + ' ';
//            }
//        }

//        return s;
//    }
//}

public class CFG
{
    private Dictionary<string, string[]> rules = new Dictionary<string, string[]>();
    Dictionary<string, int> rule_counts = new Dictionary<string, int>();

    public void AddRule(string lhs, string rhs)
    {
        string[] prods = rhs.Split(new char[] { '|' });
        for (int i = 0; i < prods.Length; ++i)
            prods[i] = prods[i].Trim();

        rules[lhs] = prods;
        rule_counts[lhs] = 0;
    }
    public string Generate(string format = "S", float recurse_factor = 0.5f)
    {
        string s = _Generate(format, recurse_factor);

        // Remove last space
        s = s.Substring(0, s.Length - 1);

        return s;
    }

    private string _Generate(string prod, float factor)
    {
        string s = "";

        foreach (string symbol in prod.Split(new char[] { ' ' }))
        {
            string[] choices;

            if (rules.TryGetValue(symbol, out choices)) // Non-terminal
            {
                ++rule_counts[symbol];

                // Find choice weights
                float[] choice_weights = new float[choices.Length];
                for (int i = 0; i < choices.Length; ++i)
                {
                    choice_weights[i] = 1;
                    foreach (string choice_symbol in choices[i].Split(new char[] { ' ' }))
                    {
                        if (!rules.ContainsKey(choice_symbol)) continue; // terminal symbols are treated as weight 1

                        // choice weight is the min of the weights of the symbols in the choice
                        choice_weights[i] = Mathf.Min(
                            choice_weights[i], Mathf.Pow(factor, rule_counts[choice_symbol]));
                    }
                }

                // Choose and recurse
                int choice_i = Tools.WeightedChoice(choice_weights);
                s += _Generate(choices[choice_i], factor);

                // Undo change to rule counts dictionary (reference type)
                --rule_counts[symbol];
            }
            else // Terminal
            {
                s += symbol + ' ';
            }
        }

        return s;
    }
}