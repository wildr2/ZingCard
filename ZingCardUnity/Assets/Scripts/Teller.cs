using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;


public class Teller : MonoBehaviour
{
    private char[] phrase_seperators = new char[] { ',', '.', '?', '!', ':', ';' };

    // Dev
    public bool dev = false;

    // General
    private GameManager gm;
    private bool showing_message = false;

    // Phrases
    private int book_number = 1;
    private string[] phrases;

    // Target Words
    private string[] target_words;
    private int[] target_word_order;
    private int target_word_order_i = -1; // ++ed to 0 on tell setup
    private string target_word;

    // Telling
    private Text tell_text;

    private bool shown_target_word = false;
    private string tell_phrase;
    private string tell_word; // the word being typed out now
    private int tell_index = 0;

    private const float tell_speed = 7.5f; // chars per second

    // Events
    public Action event_intro_done;



    // PUBLIC MODIFIERS

    public void Initialize(GameManager gm, int words_n)
    {
        this.gm = gm;
        tell_text = gm.court.tell_text;

        // Words
        GeneratePhrasesAndWords2(words_n);
        GenerateTellOrder();

        // Setup first tell
        SetupNextTell();

        // Events
        gm.event_new_state += (GameState s) =>
        {
            if (s == GameState.Intro) StartCoroutine(IntroRoutine());
            if (s == GameState.Play) StartCoroutine(TellRoutine());
            if (s == GameState.Reset)
            {
                SetupNextTell();
                if (!gm.PlayersReady()) ShowMessage("MOVE HANDS BEHIND HAND WALLS", 2, false);
            }
            if (s == GameState.PostGame)
            {
                tell_text.text = "";
                ShowMessage("GAME " + Tools.ColorRichTxt(gm.GetWinner().player_name.ToUpper(),
                    gm.GetWinner().player_color));
            }
        };
        gm.event_false_start += (Player p) =>
        {
            ShowMessage("FALSE START " + Tools.ColorRichTxt("+1", gm.GetOpponent(p).player_color));
        };
        gm.event_miss += (Player p) =>
        {
            ShowMessage("MISS " + Tools.ColorRichTxt("+1", gm.GetOpponent(p).player_color));
        };
        gm.event_correct_hit += (Player p) =>
        {
            ShowMessage("CORRECT HIT " + Tools.ColorRichTxt("+1", p.player_color));
        };
        gm.event_game_point += () =>
        {
            ShowMessage("GAME POINT");
        };
    }


    // PRIVATE MODIFIERS

    private IEnumerator IntroRoutine()
    {
        tell_text.text = "FIRST TO " + gm.GetPointsToWin();
        yield return new WaitForSeconds(3);
        tell_text.text = "GOOD LUCK HAVE FUN!";
        yield return new WaitForSeconds(3);
        tell_text.text = "";

        if (!gm.PlayersReady()) ShowMessage("MOVE HANDS BEHIND HAND WALLS", 2, false);

        if (event_intro_done != null) event_intro_done();
    }
    private void ShowMessage(string msg, float duration=3, bool flash=true)
    {
        StartCoroutine(ShowMessageRoutine(msg, duration, flash));
    }
    private IEnumerator ShowMessageRoutine(string msg, float duration, bool flash)
    {
        // Yield for earlier messages
        while (showing_message) yield return null;

        showing_message = true;
        string original_text = tell_text.text;

        if (flash)
        {
            for (int i = 0; i < 5; ++i)
            {
                if (i % 2 == 0) tell_text.text = msg;
                else tell_text.text = "";
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Leave message
        tell_text.text = msg;
        yield return new WaitForSeconds(duration - (flash ? 0.5f : 0));

        // Restore original text
        tell_text.text = original_text;
        
        showing_message = false;
    }

    private void SetupNextTell()
    {
        ++target_word_order_i;
        int i = target_word_order[target_word_order_i];

        tell_phrase = phrases[i];
        target_word = target_words[i];
        shown_target_word = false;
        tell_text.text = "";
    }
    private IEnumerator TellRoutine()
    {
        string[] tell_words = tell_phrase.Split();
        tell_word = "";
        tell_index = 0;
        int target_word_start_i = 0;
        int stop_index = tell_phrase.Length;

        // Find target word start index
        for (int i = 0; i < tell_words.Length; ++i)
        {
            if (tell_words[i] == target_word) break;
            target_word_start_i += tell_words[i].Length + 1;
        }

        while (showing_message) yield return null;
        tell_text.text = "";

        // Show '...' for a while
        ShowMessage("...", 2, false);

        for (int i = 0; i < tell_words.Length; ++i)
        {
            // Next word
            tell_word = tell_words[i];

            foreach (char c in tell_word + (i < tell_words.Length-1 ? " " : ""))
            {
                // Pause for messages
                while (showing_message) yield return null;
                if (gm.GetGameState() == GameState.PostPlay)
                {
                    stop_index = tell_index;
                    break;
                }

                // Add character
                tell_text.text += c;
                ++tell_index;
                if (tell_index == target_word_start_i+1) // tell index points to 2nd letter in target word
                    shown_target_word = true;

                yield return new WaitForSeconds(1 / tell_speed);
            }
        }

        // Wait for post play and no messages
        while (gm.GetGameState() == GameState.Play || showing_message) yield return null;

        // Show all text with highlighting
        ModStr txt = new ModStr(tell_phrase);
        txt.ColorRichTxt(target_word_start_i, target_word.Length, gm.GetLastScorer().player_color);
        txt.Insert(stop_index, Tools.ColorRichTxt("/", 0, 1, tell_text.color));
        tell_text.text = txt.Get();   
    }

    private void GenerateTellOrder()
    {
        target_word_order = Enumerable.Range(0, target_words.Length).ToArray();
        Tools.ShuffleArray(target_word_order);
    }
    private void GeneratePhrasesAndWords(int n)
    {
        CFG cfg = new CFG();
        //cfg.AddRule("S", "N V");
        //cfg.AddRule("N", "Adj p | Adj n p | n p | p");
        //cfg.AddRule("V", "adv v | adv v N | v adv | v N | v");
        //cfg.AddRule("Adj", "Adj adj | adj");
        cfg.AddRule("S", "N and N | N");
        cfg.AddRule("N", "Adj p");
        cfg.AddRule("Adj", "Adj adj | adj");


        Dictionary<string, string[]> pos_words = new Dictionary<string, string[]>();
        pos_words["n"] = File.ReadAllLines("Assets/Texts/nouns.txt");
        pos_words["p"] = File.ReadAllLines("Assets/Texts/cheap_plurals.txt");
        pos_words["v"] = File.ReadAllLines("Assets/Texts/verbs.txt");
        pos_words["adj"] = File.ReadAllLines("Assets/Texts/adjectives.txt");
        pos_words["adv"] = File.ReadAllLines("Assets/Texts/adverbs.txt");


        phrases = new string[n];
        target_words = new string[n];
        int[] target_indices = new int[n];

        //HashSet<string> used_words = new HashSet<string>();

        // Generate
        //for (int i = 0; i < n; ++i)
        //{
        //    // Format
        //    string format = cfg.Generate("S", 0.75f);

        //    // Decide target word location
        //    string[] symbols = format.Split(new char[] { ' ' });
        //    float[] weights = new float[symbols.Length];
        //    string[] phrase_words = new string[symbols.Length];
        //    phrases[i] = "";
        //    for (int j = 0; j < symbols.Length; ++j)
        //    {
        //        weights[j] = symbols[j] == "n" ? 1 :
        //                     symbols[j] == "p" ? 1 :
        //                     symbols[j] == "v" ? 0.25f :
        //                     symbols[j] == "adj" ? 0.5f :
        //                     symbols[j] == "adv" ? 0.25f : 0;

        //        string[] lines;
        //        if (pos_words.TryGetValue(symbols[j], out lines))
        //            phrase_words[j] = WordTools.RandomWord(pos_words[symbols[j]], used_words);
        //        else
        //            phrase_words[j] = symbols[j];

        //        phrases[i] += phrase_words[j] + ' ';
        //        used_words.Add(phrase_words[j]);
        //    }
        //    int target_index = Tools.WeightedChoice(weights);
        //    target_words[i] = phrase_words[target_index];

        //    // Remove last space
        //    phrases[i] = phrases[i].Substring(0, phrases[i].Length - 1);


        //    // Dev
        //    if (dev)
        //    {
        //        string debug_phrase = "";
        //        foreach (string word in phrase_words)
        //            debug_phrase += word == target_words[i] ? Tools.ColorRichTxt(word, Color.blue) + ' ' : word + ' ';
        //        Tools.Log(debug_phrase);
        //        //Tools.Log(phrases[i] + " : " + Tools.ColorRichTxt(target_words[i], Color.red));
        //    }
        //}


        //Generate phrase formats and target words
        for (int i = 0; i < n; ++i)
        {
            // Format
            phrases[i] = cfg.Generate("S", 0.75f);

            // Decide target word location
            string[] symbols = phrases[i].Split(new char[] { ' ' });
            float[] weights = new float[symbols.Length];
            for (int j = 0; j < symbols.Length; ++j)
            {
                weights[j] = symbols[j] == "n" ? 0 :
                             symbols[j] == "p" ? 1 :
                             symbols[j] == "v" ? 0f :
                             symbols[j] == "adj" ? 0f :
                             symbols[j] == "adv" ? 0f : 0;
            }
            target_indices[i] = Tools.WeightedChoice(weights);

            // Choose target word
            target_words[i] = WordTools.RandomWord(pos_words[symbols[target_indices[i]]], target_words);
        }

        // Generate phrase words
        for (int i = 0; i < n; ++i)
        {
            string[] symbols = phrases[i].Split(new char[] { ' ' });

            phrases[i] = "";
            for (int j = 0; j < symbols.Length; ++j)
            {
                if (j == target_indices[i])
                    phrases[i] += target_words[i] + ' ';
                else
                {
                    string[] lines;
                    if (pos_words.TryGetValue(symbols[j], out lines))
                        phrases[i] += WordTools.RandomWord(pos_words[symbols[j]], target_words) + ' ';
                    else
                        phrases[i] += symbols[j] + ' ';
                    //phrases[i] += WordTools.RandomWord(pos_words[symbols[j]], target_words) + ' ';
                }
            }

            // Remove last space
            phrases[i] = phrases[i].Substring(0, phrases[i].Length - 1);

            //Dev
            if (dev)
            {
                string debug_phrase = "";
                foreach (string word in phrases[i].Split(new char[] { ' ' }))
                    debug_phrase += word == target_words[i] ? Tools.ColorRichTxt(word, Color.blue) + ' ' : word + ' ';
                Tools.Log(debug_phrase);
                //Tools.Log(phrases[i] + " : " + Tools.ColorRichTxt(target_words[i], Color.blue));
            }
        }
    }
    private void GeneratePhrasesAndWords2(int n)
    {
        Pair<string[], string[]> pair = WordTools.GenPhrasesAndTargetWords(n);

        phrases = pair.First;
        target_words = pair.Second;
    }

    private void PickWords(int n)
    {
        target_words = new string[n];
        string alltext = File.ReadAllText("Assets/Texts/Local Books/Book" + book_number + ".txt");

        int attempts = 0;
        while (attempts < 5)
        {
            // find n phrases
            phrases = new string[n];
            for (int i = 0; i < n; ++i)
            {
                string p = "";
                do
                {
                    p = FindPhrase(alltext);
                } while (p.Length > 90);
                phrases[i] = p;
            }

            // find target_words
            PickUniqueWords(n);

            // check whether unique target_words were found for each phrase
            bool allgood = true;
            for (int i = 0; i < n; ++i)
            {
                if (target_words[i] == "")
                {
                    allgood = false;
                    break;
                }
            }
            if (allgood) break;
            attempts++;
        }
    }
    private void PickWordsDebug(int n)
    {
        phrases = new string[n];
        target_words = new string[n];

        for (int i = 0; i < n; ++i)
        {
            phrases[i] = "contains a word";
            target_words[i] = "word";
        }
    }
    private void PickUniqueWords(int n)
    {
        // find word frequencies (unique or no)
        Dictionary<string, int> seen_words = new Dictionary<string, int>();
        for (int i = 0; i < n; ++i)
        {
            foreach (string word in phrases[i].Split(new char[] { ' ', '\t', '\r', '\n' }))
            {
                if (seen_words.ContainsKey(word)) seen_words[word] = -1;
                else seen_words[word] = i;
            }
        }

        // init word list
        target_words = new string[n];

        // find and save unique target_words
        foreach (KeyValuePair<string, int> kv in seen_words)
        {
            if (kv.Value != -1) target_words[kv.Value] = kv.Key;
        }
    }


    // PRIVATE HELPERS

    

    private bool IsGoodWord(string word)
    {
        return word.Length >= 3 && !target_words.Contains(word);
    }
    private string FindWord(string phrase)
    {
        string word = "";
        string[] phrase_words = phrase.Split(new char[] { ' ', '\t', '\r', '\n' });

        // choose random word 
        int word_index = UnityEngine.Random.Range(0, phrase_words.Length);

        while (true)
        {
            word = phrase_words[word_index];
            if (IsGoodWord(word)) return word;
            else
            {
                word_index++;
                if (word_index >= phrase_words.Length)
                {
                    // bad phrase
                    return "";
                }
            }
        }
    }
    private string FindPhrase(string alltext)
    {
        string phrase = "";

        int j = UnityEngine.Random.Range(0, alltext.Length);
        while (!phrase_seperators.Contains(alltext[j]))
        {
            ++j; 
        }
        while (phrase_seperators.Contains(alltext[j]))
        {
            ++j;
        }

        while (true)
        {
            phrase += alltext[j];
            ++j;
            if (phrase_seperators.Contains(alltext[j])) break;
        }

        // cleanup the phrase
        phrase = phrase.Replace("\n", " ");
        phrase = phrase.Replace("\r\n", " ");
        phrase = phrase.Replace("\r", " ");
        phrase = phrase.Replace("'", "");
        phrase = phrase.Replace("\"", "");
        phrase = phrase.Replace("\t", "");
        phrase = phrase.Replace("  ", " ");
        phrase = phrase.Trim();
        phrase = phrase.ToLower();

        return phrase;
    }

    


    // PUBLIC ACCESSORS

    public bool HasShownTargetWord()
    {
        return shown_target_word;
    }
    public string GetTargetWord()
    {
        return target_word;
    }
    public string GetTargetWord(int index)
    {
        return target_words[index];
    }
    public int GetTargetWordIndex()
    {
        return target_word_order[target_word_order_i];
    }
    public string GetTellPhrase()
    {
        return tell_phrase;
    }
    public string GetTellWord()
    {
        return tell_word;
    }
    public int GetTellPhraseIndex()
    {
        return tell_index;
    }
    public int GetNumUntold()
    {
        return target_words.Length - target_word_order_i;
    }

}


