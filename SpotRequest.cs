using System.Collections;
using RootMotion.FinalIK; // Ragdoll plugin
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

public class SpotRequest : MonoBehaviour
{
    public static SpotRequest SR;   // Static instance of this Class accessible from all project

    [Header("Level Preferences")]
    [Space(10)]
    public bool randomPick = true;  // True for random request spots, False for pre defined Spots
    public bool showFinger = true;  // Show finger UI
    public int levelLenght = 10;    // number of spot requests by level
    public float waitTime = 1f;

    [Header("GameObjects References")]
    [Space(10)]
    public TwisterPlayer twisterPlayer;     // Reference to main player script
    public TwisterPlayer twisterPlayerAI;   // Reference to AI player script
    public Timer timer;                     // Reference to timer script
    public GameObject Confetti;             //Confetti particle effect

    [Header("UI Groups")]
    [Space(10)]
    public GameObject fingerUi;
    public Image spotColorUI;               // UI image to show requested color
    public GameObject WinUI;                // Win user interface group
    public GameObject LooseUI;              // Loose user interface group


    [Header("Lists of Resources")]
    [Space(10)]
    public GameObject[] members;            //Body members (Left hand, right hand, left foot, right foot )
    public AudioClip[] mSounds;             // Sounds / voice recording of body members
    
    public Color[] colors;                  // Spot Colors
    public AudioClip[] cSounds;             // Sounds / Voice recording of colors

    public GameObject[] UiMarkers;          // UI Markers to highlight requested body member + color

    public Vector2[] AI;                    // pre defined list of spot request (Body member index - color index) if RandomPick is false
    public InteractionObject[] AIspots;     // pre defined target spots for AI player  if == randomPick is false


    int memberRequest, colorRequest,playedSpot,progress = 0; // internal variable to store temporarly selected values
    bool Playing = false; // is game going or not
    AudioSource audioSource; 

    private void Start()
    {
        /// Start function to setup components references

        SR = this;
        audioSource = GetComponent<AudioSource>();
        if (!showFinger) fingerUi.SetActive(false);
    }
    public void StartGame()
    {
        /// Public function to start playing
        
        StartCoroutine(LevelRequests());
    }
    IEnumerator LevelRequests()
    {
        /// Check playing status, wait and request next play
        
        if (!Playing)
        {
            Playing = true;
        }else
        {
            yield return new WaitForSeconds(waitTime);
        }
        GetRequest();
    }
    void ResetRequest()
    {
        /// Reset and clear previous play.
        
        spotColorUI.gameObject.SetActive(false);

        foreach (GameObject member in members)
        {
            member.SetActive(false);
        }
        foreach (GameObject marker in UiMarkers)
        {
            marker.SetActive(false);
        }
    }
    void GetRequest()
    {
        /// Get Random or pre defined request ( body member - color )
        
        ResetRequest(); // Clear previous request

        if (randomPick)
        {
            memberRequest = Random.Range(0, members.Length);
            colorRequest = Random.Range(0, colors.Length);
        }
        else
        {
            memberRequest = (int)AI[progress].x;
            colorRequest = (int)AI[progress].y;
        }
        
        if (showFinger) fingerUi.SetActive(true);

        if (Playing) PlayRequest();
    }
    void PlayRequest()
    {
        /// Play the current request
        /// 
        // set colors on UI spot annoucer and body member marker UI

        spotColorUI.color = colors[colorRequest];
        UiMarkers[memberRequest].GetComponent<MemberUI>().SetColor(colors[colorRequest]);
        UiMarkers[memberRequest].SetActive(true);
        members[memberRequest].SetActive(true);
        spotColorUI.gameObject.SetActive(true);

        // Notify Player and AI Player of which body member requested for the current play.

        twisterPlayer.SetMember(members[memberRequest].name);
        if (twisterPlayerAI.isActiveAndEnabled) twisterPlayerAI.AIplay(members[memberRequest].name, AIspots[progress]);

        // Start Timer and voice annoucer

        if (timer.isActiveAndEnabled) timer.StartTimer();
        StartCoroutine(Voice());
    }
    
    public void SetPlayedSpot(int spot)
    {
        /// Set the played spot by the player.
        
        playedSpot = spot;
    }
    public void CheckResult()
    {
        /// Check result by comparing played spot and requested spot.
        
        if (showFinger) fingerUi.SetActive(false); // Hide finger UI marker

        // Compare played spot by player earlier with the requested play

        if (playedSpot == colorRequest)
        {
            Debug.Log("Correct Play");

            twisterPlayer.ReachSpotSucceed();
            UiMarkers[memberRequest].GetComponent<MemberUI>().SetResult(1); // UI marker has 2 results ( 1 for success effect, 0 for fail effect)
        }
        else
        {
            Debug.Log("Wrong Play");

            twisterPlayer.Fall();
            UiMarkers[memberRequest].GetComponent<MemberUI>().SetResult(0); // UI marker has 2 results ( 1 for success effect, 0 for fail effect)

            Finish(false);
        }

        // check progress on current level

        if ((!randomPick && progress < AI.Length - 1 && progress < levelLenght) || (randomPick && progress < levelLenght))
        {
            // keep playing

            progress++;
            if (Playing) StartCoroutine(LevelRequests());
        }
        else
        {
            Finish(true);
        }
        
    }
    public void Finish(bool win)
    {
        /// public function to finish game
        /// stop the game and show UI based on game result

        if (Playing)
        {
            ResetRequest();
            Playing = false;
            StopCoroutine(Voice());
            Destroy(timer.gameObject);

            if (win)
            {
                twisterPlayer.Win();        // Player wins
                twisterPlayerAI.Fall();     // Enemy or Player AI looses
                Confetti.SetActive(true);
            }
            else
            {
                twisterPlayer.Fall();       // Player looses
                twisterPlayerAI.Win();      // Enemy or Player AI wins
            }
        }
        
    }
    
    IEnumerator Voice()
    {
        /// Voice announcer of requested play ( body member - color )
        
        audioSource.PlayOneShot(mSounds[memberRequest]);
        yield return new WaitForSeconds(waitTime);
        audioSource.PlayOneShot(cSounds[colorRequest]);
    }
    
}
