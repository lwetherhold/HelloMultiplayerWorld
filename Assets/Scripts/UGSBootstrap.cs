using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;

public class UGSBootstrap : MonoBehaviour
{
    // initialize the unity services and sign in anonymously
    private async void Awake()
    {
        // initialize the unity services
        await UnityServices.InitializeAsync();
        // sign in anonymously if not already signed in
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
}