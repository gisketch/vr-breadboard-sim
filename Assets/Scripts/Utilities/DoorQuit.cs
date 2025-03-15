using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DoorQuit : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    private bool isHovering = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    void Update()
    {
        if (isHovering && InputManager.Instance.GetPrimaryButton())
        {
            // Properly shutdown network before scene reset
            // DisconnectAndResetScene();
            Application.Quit();
        }
    }

    private void DisconnectAndResetScene()
    {
        // Check if we're connected to a network session
        if (Mirror.NetworkClient.isConnected)
        {
            // Stop client if we're a client
            Mirror.NetworkClient.Disconnect();
        }

        // Stop host if we're a host/server
        if (Mirror.NetworkServer.active)
        {
            Mirror.NetworkManager.singleton.StopHost();
        }

        // Wait briefly for network to clean up, then reset scene
        StartCoroutine(DelayedReset());
    }

    private IEnumerator DelayedReset()
    {
        // Wait for network to clean up
        yield return new WaitForSeconds(0.5f);
        GameManager.Instance.ResetScene();
    }
}
