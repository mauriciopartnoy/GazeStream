using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Maw.GlobalEvents;

public class WebSocketStatus : MonoBehaviour
{
    [SerializeField] Image img;
    [SerializeField] TMP_Text portText;

    private void OnEnable()
    {
        OnStatusChanged();
        GlobalEvents.OnWebsocketStatusChanged.Add(OnStatusChanged);
    }

    private void OnDisable()
    {
        GlobalEvents.OnWebsocketStatusChanged.Remove(OnStatusChanged);
    }

    void OnStatusChanged()
    {
        if (WebSocketStream.I.IsAlive)
        {
            SetConnectedStatus();
        }
        else
        {
            SetDisconnectedStatus();
        }
    }

    void SetConnectedStatus()
    {
        img.color = Color.green;
        portText.text = WebSocketStream.WEBSOCKET_PORT;
    }

    void SetDisconnectedStatus()
    {
        img.color = Color.red;
        portText.text = "Disconnected";
    }

    public void TryReconnect()
    {
        WebSocketStream.I.TryConnect();
    }
}
