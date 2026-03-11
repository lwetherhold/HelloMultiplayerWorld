using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;

public class UGSBootstrap : MonoBehaviour
{
    private async void Awake()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
}