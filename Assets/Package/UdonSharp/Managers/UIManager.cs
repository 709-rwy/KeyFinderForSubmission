
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class UIManager : UdonSharpBehaviour
{

    // ========
    //  FIELDS
    // ========

    [SerializeField] private GameObject container;
    [SerializeField] private TextMeshProUGUI timer;
    [SerializeField] private TextMeshProUGUI keycounter;

    [SerializeField] private Vector3 trackPosition;

    private bool isPlayerINVR;

    // ===================
    //  LIFECYCLE METHODS
    // ===================

    void Start()
    {
        ShowUI(false);
        isPlayerINVR = Networking.LocalPlayer.IsUserInVR();
    }
    void Update()
    {
        Track();
    }

    // ====================
    //  METHODS SETTING UI
    // ====================

    public void SetTimer(int second)
    {
        int minute = second / 60;
        second %= 60;

        timer.text = string.Format("{0}:{1:d2}", minute, second);
    }

    public void SetKeycount(int keycount, int lockNum)
    {
        keycounter.text = string.Format("{0}/{1}", keycount, lockNum);
    }

    // ===============
    //  OTHER METHODS
    // ===============

    private void Track()
    {
        VRCPlayerApi.TrackingData head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        gameObject.transform.position = head.position + (head.rotation * trackPosition);

        if(isPlayerINVR)
        {
        gameObject.transform.rotation = head.rotation *Quaternion.LookRotation(trackPosition.normalized);
        }
        else
        {
            gameObject.transform.rotation = head.rotation;
        }
    }

    public void ShowUI(bool boo)
    {
        container.SetActive(boo);
    }
}
