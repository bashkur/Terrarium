using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickTimeEvents
{
    public bool activated;
    public GameObject player;
    public ZombieScript target;
    public Canvas can;
    public typeOfQTE type;

    public QuickTimeEvents(GameObject _player, ZombieScript _target, Canvas _can) { player = _player; target = _target; can = _can; }

    virtual public void Start() { }
    virtual public void Update() { }
}

public enum typeOfQTE
{
    SpamButton,
    PreciseClick
};

public class PreciseClickEvent : QuickTimeEvents
{
    public Vector2 spawnLocation;
    public Vector4 bounds;
    public float radius;

    public float timeLeft;
    private float startingTime;

    public PreciseClickEvent(GameObject _player, ZombieScript _target, Canvas _can) : base(_player, _target, _can)
    {

    }

    public override void Start()
    {
        base.Update();
        startingTime = timeLeft;
    }

    public override void Update()
    {
        base.Update();
        timeLeft -= Time.deltaTime;
    }
   
}

public class SpamButtonEvent: QuickTimeEvents
{
    public KeyCode keyCode;
    public TextMeshProUGUI keyTextMesh;
    //public GameObject parent;

    public GameObject quickTimeBar;
    //public Image BackgroundBar;
    public Image HumanSide;
    public Image ZombieSide;
    public Image MiddleBar;

    public float threshold = 0.1f;
    public float coeffecient = 0.25f;

    public AnimationCurve zombieForce;
    public float fillThreshold = 0.85f;
    private float time;
    private bool down;

    private float timeElapse;
    private int numPresses;
    private float rate;
    private float maxCurveVal;

    private float maxHeight;
    private GameObject unPressed, pressed;
    private float delay = 0.005f;
    private bool start = false;
    private bool swap = false;

    public SpamButtonEvent(GameObject _player, ZombieScript _target, Canvas _can) : base (_player, _target, _can)
    {
        type = typeOfQTE.SpamButton;
        keyCode = KeyCode.A +  (int)UnityEngine.Random.Range(0.0f, 25.0f);

        quickTimeBar = target.quickTimeBar;

        //GameObject instantiatedGameObj = GameObject.Instantiate(quickTimeBar, new Vector3(0, 0, 0), Quaternion.identity);
        quickTimeBar.SetActive(true);

        zombieForce = target.zombieForce;

        pressed = quickTimeBar.transform.GetChild(1).transform.GetChild(0).gameObject;
        pressed.SetActive(false);
        unPressed = quickTimeBar.transform.GetChild(1).transform.GetChild(1).gameObject;
        keyTextMesh = quickTimeBar.transform.GetChild(1).transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>();

        keyTextMesh.text = "" + keyCode.ToString();

        maxHeight = quickTimeBar.transform.GetComponent<RectTransform>().rect.height;

        GameObject temp = quickTimeBar.transform.GetChild(0).gameObject;
        HumanSide = temp.GetComponent<Image>();
        temp = temp.transform.GetChild(0).gameObject;
        ZombieSide = temp.GetComponent<Image>();
        temp = temp.transform.GetChild(0).gameObject;
        MiddleBar = temp.GetComponent<Image>();

        HumanSide.fillAmount = 0.5f;
        ZombieSide.fillAmount = 0.5f;

        rate = 0;

        maxCurveVal = zombieForce[zombieForce.length - 1].value;

        keyTextMesh.enabled = true;
        //Debug.Log("yo");

        Start();
    }

    public override void Start()
    {
        base.Start();
        time = 0.0f;
        down = false;
        //Debug.Log( keyCode);
    }

    public override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(keyCode) && !down)
        {
            timeElapse = time;
            down = true;
            start = true;
        }
        
        if (Input.GetKeyUp(keyCode))
        {
            rate = time - timeElapse;
            numPresses++;
            HumanSide.fillAmount += 0.01f * ((rate < 0.8f) ? 1.5f : 1) * ((rate < 0.25f) ? 2 : 1);
            down = false;
        }

        if(start && delay <= 0)
        {
            pressed.SetActive(!swap);
            unPressed.SetActive(swap);
            delay = 0.25f;
            swap = !swap;
        }

        delay -= Time.deltaTime;

        if (HumanSide.fillAmount >= fillThreshold)
        {
            Debug.Log("player won");
            GameManager.Instance.UpdateScore(10);
            target.onComplete(true);
            quickTimeBar.SetActive(false);
            //keyTextMesh.enabled = false;
            return;
        }
        else if (HumanSide.fillAmount < 1 - fillThreshold)
        {
            Debug.Log("zombie won");
            target.onComplete(false);
            quickTimeBar.SetActive(false);
            //keyTextMesh.enabled = false;
            return;
        }
        
        if(rate!= 0 && !down)
        {
            rate = time - timeElapse;
        }
        //* ((rate < 0.05f) ? 0.05f : 1)
        float delta = zombieForce.Evaluate(time)/ maxCurveVal * Time.deltaTime * coeffecient * ((rate != 0)? 1 : 0) * ((rate < 0.8f) ? 0.5f : 1);
        HumanSide.fillAmount -= delta;
        ZombieSide.fillAmount = 1 - HumanSide.fillAmount;

        MiddleBar.gameObject.transform.localPosition = new Vector3(0, maxHeight * HumanSide.fillAmount, 0);

        time += Time.deltaTime;
    }

    void OnDestroy()
    {
        quickTimeBar.SetActive(false);
    }
}
