using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoundedEdge : MonoBehaviour
{

    public Image fillBar;
    private string fillOriginName;
    private float length;
    private float myLength;
    private RectTransform rectangle;
    private bool enabled = false;

    void OnEnable()
    {
        fillOriginName = "";

        switch ((Image.FillMethod)fillBar.fillMethod)
        {
            case Image.FillMethod.Horizontal:
                fillOriginName = ((Image.OriginHorizontal)fillBar.fillOrigin).ToString();
                break;
            case Image.FillMethod.Vertical:
                fillOriginName = ((Image.OriginVertical)fillBar.fillOrigin).ToString();
                break;
            case Image.FillMethod.Radial90:

                fillOriginName = ((Image.Origin90)fillBar.fillOrigin).ToString();
                break;
            case Image.FillMethod.Radial180:

                fillOriginName = ((Image.Origin180)fillBar.fillOrigin).ToString();
                break;
            case Image.FillMethod.Radial360:
                fillOriginName = ((Image.Origin360)fillBar.fillOrigin).ToString();
                break;
        }
        Debug.Log(string.Format("{0} is using {1} fill method with the origin on {2}", name, fillBar.fillMethod, fillOriginName));

        length = fillBar.gameObject.GetComponent<RectTransform>().rect.width;

        rectangle = gameObject.GetComponent<RectTransform>();

        myLength = rectangle.rect.width;

        enabled = true;
    }


    public void updateFill()
    {
        if (enabled)
        {
            float moveAmount = fillBar.fillAmount * length * length /myLength;

            switch ((Image.FillMethod)fillBar.fillMethod)
            {
                case Image.FillMethod.Horizontal:

                    rectangle.localPosition = new Vector3(moveAmount, 0, 0);

                    break;
                case Image.FillMethod.Vertical:

                    rectangle.localPosition = new Vector3(0, moveAmount, 0);

                    break;

                default:
                    break;
            }

            //Debug.LogFormat("Pos: {0}, Amount: {1}, Length: {2}", rectangle.localPosition, moveAmount, myLength);
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
