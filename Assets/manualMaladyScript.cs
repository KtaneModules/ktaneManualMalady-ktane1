using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class manualMaladyScript : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
	public KMBombModule module;
    public AudioSource audioSource;

    public KMSelectable button;
    public KMSelectable submit;
    public TextMesh screenText;
    public GameObject[] audioShufflers;
    public AudioClip[] moduleNames;

    private List<int> shuffler = new List<int>();
    private int selectedModule;
    private int selected = -1;
    private bool isAnimating;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
    	moduleId = moduleIdCounter++;

        button.OnInteract += delegate {
            ButtonFunction();
            button.AddInteractionPunch(0.25f);
            return false;
        };

        submit.OnInteract += delegate
        {
            submitHandler();
            submit.AddInteractionPunch(0.4f);
            return false;
        };

        for (int i = 0; i < audioShufflers.Length; i++)
        {
            int j = i;
            audioShufflers[j].GetComponent<KMSelectable>().OnInteract += () => { shufflerHandler(j); return false; };
        }

        module.OnActivate += Activate;
    }

    void Start()
    {
        selectedModule = UnityEngine.Random.Range(0, moduleNames.Length);
        screenText.text = moduleNames[selectedModule].name;
        StartCoroutine(resizeName(screenText.text));
        Debug.LogFormat("[Manual Malady #{0}] The chosen module is {1}.", moduleId, screenText.text);

        var sb = new StringBuilder();
        for (int i = 0; i < audioShufflers.Length; i++)
        {
            shuffler.Add(i);
        }
        shuffler.Shuffle();

        for (int i = 0; i < shuffler.Count; i++)//Logging
        {
            sb.Append((shuffler[i]+1) + ", ");
        }
        sb.Remove(sb.Length - 2, 2);
        Debug.LogFormat("[Manual Malady #{0}] The order of audio clips are shuffled as follows: {1}", moduleId, sb.ToString());
    }

    void Activate()
    {
        StartCoroutine(displayAnim());
    }

    void ButtonFunction()
    {
        if (moduleSolved) { return; }
        audio.PlaySoundAtTransform("Press", transform);
        if (audioSource.isPlaying) 
        { 
            audioSource.Stop();
            StopCoroutine("player");
            screenText.text = "";
            isAnimating = false;
            Debug.LogFormat("<Manual Malady #{0}> Play button pressed while audio clips are played, stopping audio clips...", moduleId);
            return; 
        }
        StopCoroutine("displayAnim");
        screenText.color = Color.black;
        Debug.LogFormat("<Manual Malady #{0}> Play button pressed, playing audio clips from left to right...", moduleId);
        StartCoroutine("player");
    }

    void submitHandler()
    {
        while (audioSource.isPlaying || isAnimating || moduleSolved) { return; }
        audio.PlaySoundAtTransform("Press", transform);
        var sb = new StringBuilder();
        bool toSolve = true;
        for (int i = 0; i < audioShufflers.Length; i++)
        {
            if (shuffler[i] != i) { toSolve = false; }
            sb.Append((shuffler[i]+1) + ", ");
        }
        sb.Remove(sb.Length - 2, 2);
        Debug.LogFormat("[Manual Malady #{0}] Submit button pressed with audio clips in the following order: {1} ", moduleId, sb.ToString());
        if (toSolve)
        {
            module.HandlePass();
            audio.PlaySoundAtTransform("Solve", transform);
            Debug.LogFormat("[Manual Malady #{0}] The clips are in the correct order, module solved!", moduleId);
            moduleSolved = true;
            StartCoroutine("displayAnim");
            for (int i = 0; i < audioShufflers.Length; i++)
                StartCoroutine(buttonAnim(i));
        }
        else
        {
            module.HandleStrike();
            Debug.LogFormat("[Manual Malady #{0}] The clips are in the wrong order, strike!", moduleId);
            StartCoroutine("displayAnim");
        }
    }

    void shufflerHandler(int k)
    {
        if (isAnimating || moduleSolved) { return; }
        if (selected == -1) { selected = k; audio.PlaySoundAtTransform("Select", transform); }
        else
        {
            Material tempMat = audioShufflers[k].GetComponent<MeshRenderer>().material;
            int temp = shuffler[k];
            shuffler[k] = shuffler[selected];
            audioShufflers[k].GetComponent<MeshRenderer>().material = audioShufflers[selected].GetComponent<MeshRenderer>().material;
            shuffler[selected] = temp;
            audioShufflers[selected].GetComponent<MeshRenderer>().material = tempMat;
            Debug.LogFormat("<Manual Malady #{0}> Shuffled {1} and {2}!", moduleId, selected+1, k+1);
            audio.PlaySoundAtTransform("Shuffled", transform);
            selected = -1;
        }
    }


    IEnumerator player()//Be my guest if you know how to duplicate a section of an audio clip properly (the method still have some small overlaps which causes a glitchy effect in between consecutive clips)
    {
        isAnimating = true;
        float dividedLength = moduleNames[selectedModule].length / audioShufflers.Length;
        
        for (int i = 0; i < audioShufflers.Length; i++)
        {
            //audioSource.clip = moduleNames[selectedModule];
            //audioSource.time = dividedLength * shuffler[i];
            screenText.color = audioShufflers[i].GetComponent<MeshRenderer>().material.color;
            screenText.characterSize = 1f;
            screenText.text = "PLAYING";
            audioSource.clip = MakeSubclip(moduleNames[selectedModule], dividedLength * shuffler[i], dividedLength);
            audioSource.Play();
            while (audioSource.isPlaying)
            {
                if (audioSource.time >= dividedLength /** (shuffler[i]+1)*/)
                {
                    audioSource.Stop();
                }
                yield return null;
            }
        }
        screenText.text = "";
        isAnimating = false;
    }

    IEnumerator resizeName(string k)
    {
        while (!audioSource.isPlaying)//For some reason the code won't work when it's only ran once
        {
            float width = 0;
            foreach (char symbol in k)
            {
                CharacterInfo info;
                if (screenText.font.GetCharacterInfo(symbol, out info, screenText.fontSize, screenText.fontStyle))
                {
                    width += info.advance;
                }
            }
            width = width * screenText.characterSize;
            while (width > 3000f)
            {
                screenText.characterSize -= 0.01f;
                width = 0f;
                foreach (char symbol in k)
                {
                    CharacterInfo info;
                    if (screenText.font.GetCharacterInfo(symbol, out info, screenText.fontSize, screenText.fontStyle))
                    {
                        width += info.advance;
                    }
                }
                width = width * screenText.characterSize;
            }
            yield return null;
        }
    }

    IEnumerator displayAnim()
    {
        float delta = 0f;
        float rnd = 0f;
        Color c = new Color();
        if (!moduleSolved)
        {
            screenText.text = moduleNames[selectedModule].name;
            StartCoroutine(resizeName(screenText.text));
            screenText.color = Color.white;
            yield return new WaitForSeconds(1.2f);
            audio.PlaySoundAtTransform("Glitch", transform);
            while (delta < 1f)
            {
                delta += Time.deltaTime * 1 / 0.544f;
                rnd = UnityEngine.Random.Range(0.1f, 0.5f);
                c = new Color(rnd, rnd, rnd, 1f);
                screenText.color = Color.Lerp(Color.white, Color.black, delta) + c;
                yield return null;
            }
            screenText.color = Color.black;
        }
        else
        {
            screenText.color = Color.white;
            screenText.characterSize = 1f;
            screenText.text = "MODULE SOLVED";
            while (delta < 5f)
            {
                delta += Time.deltaTime;
                rnd = UnityEngine.Random.Range(0.4f, 0.8f);
                screenText.color = new Color(rnd, rnd, rnd, 1f); 
                yield return null;
            }
            audio.PlaySoundAtTransform("Glitch", transform);
            screenText.color = Color.black;
        }
    }

    IEnumerator buttonAnim(int k)
    {
        isAnimating = true;
        float delta = 0f;
        Color originalColor = audioShufflers[k].GetComponent<MeshRenderer>().material.color;
        while (delta < 1f)
        {
            delta += Time.deltaTime;
            audioShufflers[k].GetComponent<MeshRenderer>().material.color = Color.Lerp(originalColor, Color.gray, delta);
            yield return null;
        }
        isAnimating = false;
    }

    private AudioClip MakeSubclip(AudioClip clip, float start, float timeLength)
    {
        /* Create a new audio clip */
        int frequency = clip.frequency;
        int samplesLength = (int)(frequency * timeLength * clip.channels);
        AudioClip newClip = AudioClip.Create(clip.name + "-sub", samplesLength, clip.channels, frequency, false);
        /* Create a temporary buffer for the samples */
        float[] data = new float[samplesLength];
        /* Get the data from the original clip */
        clip.GetData(data, (int)(frequency * start));
        /* Transfer the data to the new clip */
        newClip.SetData(data, 0);
        /* Return the sub clip */
        return newClip;
    }

    //Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"<!{0} play> to play the audio clips, <!{0} submit> to submit the current order, <!{0} swap 1 2> to swap the 1st and 2nd buttons, multiple swaps can be done by separating each pair with a semicolon, e.g. <!{0} swap 1 2;3 4;5 6>";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        var parameters = command.Split(' ');
        yield return null;
        if (Regex.IsMatch(command, @"^\s*play\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            button.OnInteract();
            yield return null;
        }
        else if (Regex.IsMatch(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (isAnimating) { yield return "sendtochaterror The module is playing audio clips right now."; yield break; }
            submit.OnInteract();
            yield return null;
        }
        else if (Regex.IsMatch(parameters[0], @"^\s*swap\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (isAnimating) { yield return "sendtochaterror The module is playing audio clips right now."; yield break; }
            command = command.Substring(4).Trim();
            var pairs = command.Split(';').Select(a => a.Trim()).ToArray();
            foreach (string s in pairs)
            {
                var b = s.Split();
                if (b.Length != 2) { yield return "sendtochaterror Invalid command, please try again!"; yield break; }
                for (int i = 0; i < b.Length; i++)
                {
                    int n = 0;
                    bool c = int.TryParse(b[i], out n);
                    if (!c) { yield return "sendtochaterror Invalid button to swap, please try again!"; yield break; }
                    if (n < 1 || n > 8)
                    {
                        yield return "sendtochaterror Invalid button to swap, please try again!"; yield break;
                    }
                }
            }
            for (int i = 0; i < pairs.Length; i++)
            {
                for (int j = 0; j < 3; j += 2)//Funny way of getting 0th and 2nd character of each pair
                {
                    audioShufflers[pairs[i][j] - '0' - 1].GetComponent<KMSelectable>().OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        else { yield return "sendtochaterror Invalid command, please try again!"; yield break; }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            if (isAnimating) { button.OnInteract(); yield return null; }
            for (int i = 0; i < 8; i++)
            {
                audioShufflers[i].GetComponent<KMSelectable>().OnInteract();
                yield return null;
                audioShufflers[shuffler.IndexOf(i)].GetComponent<KMSelectable>().OnInteract();
                yield return null;
            }
            submit.OnInteract();
            yield return null;
        }
        yield return null;
    }
}
