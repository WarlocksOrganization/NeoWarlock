using System;
using System.Collections;
using System.Net;
using System.Text;
using Networking;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class OauthUI : MonoBehaviour
{
    // SSAFY OAuth2 Configuration
    private const string SSAFYClientId = "4923466d-06fb-4297-beca-35cc73b7bc6d";
    private const string SSAFYRedirectUri = "http://localhost:45091/provider/ssafy/callback";
    private const string SSAFYAuthorizationEndpoint = "https://project.ssafy.com/oauth/sso-check";
    private const string SSAFYTokenEndpoint = "https://project.ssafy.com/ssafy/oauth2/token";
    private const string SSAFYUserInfoEndpoint = "https://project.ssafy.com/ssafy/resources/userInfo";

    // Google OAuth2 Configuration
    private const string GoogleClientId = "350523125550-1h2ak96sh68edvpsn73pbfib6nu952pp.apps.googleusercontent.com";
    private const string GoogleRedirectUri = "http://localhost:45091/provider/google/callback";
    private const string GoogleAuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string GoogleTokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GoogleUserInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";

    private Queue listenerQueue = new Queue();
    private HttpListener httpListener;

    // Start SSAFY OAuth2 Flow
    public void StartSSAFYOAuthFlow()
    {
        StartHttpListener(SSAFYRedirectUri);
        string authorizationUrl = $"{SSAFYAuthorizationEndpoint}?response_type=code&client_id={SSAFYClientId}&redirect_uri={SSAFYRedirectUri}";
        Application.OpenURL(authorizationUrl);
    }

    // Start Google OAuth2 Flow
    public void StartGoogleOAuthFlow()
    {
        StartHttpListener(GoogleRedirectUri);
        string authorizationUrl = $"{GoogleAuthorizationEndpoint}?" +
                                  $"response_type=code&" +
                                  $"client_id={GoogleClientId}&" +
                                  $"redirect_uri={GoogleRedirectUri}&" +
                                  $"scope=email%20profile";
        Application.OpenURL(authorizationUrl);
    }

    // 로컬 서버에서 OAuth2 리디렉션을 수신하기 위해 HttpListener 시작
    private void StartHttpListener(string redirectUri)
    {
        try
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(redirectUri + "/");
            httpListener.Start();
            httpListener.BeginGetContext(OnHttpRequest, null);
            Debug.Log("[OauthUI] Listening for OAuth2 redirect on " + redirectUri);
        }
        catch (Exception ex)
        {
            Debug.LogError("[OauthUI] Failed to start HttpListener: " + ex.Message);
        }
    }

    // HttpListener 요청 처리
    private void OnHttpRequest(IAsyncResult result)
    {
        try
        {
            if (httpListener == null || !httpListener.IsListening)
            {
                Debug.LogWarning("[OauthUI] HttpListener is not running.");
                return;
            }

            var context = httpListener.EndGetContext(result);
            string authorizationCode = context.Request.QueryString["code"];

            // 브라우저 창 닫기
            string responseString = "<html><body><h1>Auth Complete!</h1><p>You can close the window now.</p></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();

            Debug.Log("[OauthUI] Authorization Code received: " + authorizationCode);

            // HttpListener 종료
            httpListener.Stop();
            httpListener.Close();

            // 인증 코드를 토큰으로 교환
            if (context.Request.Url.AbsoluteUri.Contains("google"))
            {
                listenerQueue.Enqueue("google " + authorizationCode);
                Debug.Log("google " + authorizationCode);
            }
            else
            {
                listenerQueue.Enqueue("ssafy " + authorizationCode);
                Debug.Log("ssafy " + authorizationCode);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[OauthUI] Error handling HTTP request: " + ex.Message);
        }
    }

    // SSAFY 코드를 토큰으로 교환
    private void ExchangeSSAFYCodeForToken(string authorizationCode)
    {
        StartCoroutine(GetAccessToken(authorizationCode, SSAFYTokenEndpoint, SSAFYRedirectUri, SSAFYClientId, GetSSAFYSecret(), SSAFYUserInfoEndpoint));
    }

    // 구글 코드를 토큰으로 교환
    private void ExchangeGoogleCodeForToken(string authorizationCode)
    {
        StartCoroutine(GetAccessToken(authorizationCode, GoogleTokenEndpoint, GoogleRedirectUri, GoogleClientId, GetGoogleSecret(), GoogleUserInfoEndpoint));
    }

    // 접근 토큰 가져오기
    private IEnumerator GetAccessToken(string authorizationCode, string tokenEndpoint, string redirectUri, string clientId, string clientSecret, string userInfoEndpoint)
    {
        WWWForm form = new WWWForm();
        form.AddField("grant_type", "authorization_code");
        form.AddField("code", authorizationCode);
        form.AddField("redirect_uri", redirectUri);
        form.AddField("client_id", clientId);
        if (!string.IsNullOrEmpty(clientSecret))
        {
            form.AddField("client_secret", clientSecret);
        }

        using (UnityWebRequest request = UnityWebRequest.Post(tokenEndpoint, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[OauthUI] Access Token Response: " + request.downloadHandler.text);
                string accessToken = ParseAccessToken(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(userInfoEndpoint))
                {
                    StartCoroutine(GetUserInfo(accessToken, userInfoEndpoint));
                }
            }
            else
            {
                Debug.LogError("[OauthUI] Error fetching access token: " + request.error);
            }
        }
    }

    private string GetSSAFYSecret()
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String("NDZmYjQzZWYtNDZjYy00MzJhLTgxNjItYTdjNDczOGNiYmZk"));
    }

    private string GetGoogleSecret()
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String("R09DU1BYLVhoQXFsNnZkRGZteVhXdXoyNnFzc0NUME1MdGQ="));
    }

    // Fetch User Info
    private IEnumerator GetUserInfo(string accessToken, string userInfoEndpoint)
    {
        UnityWebRequest request = UnityWebRequest.Get(userInfoEndpoint);
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            if (userInfoEndpoint.Contains("ssafy"))
            {
                string identifier = "ssafy"+ request.downloadHandler.text.Split(',')[0].Split(':')[1].Replace("\"", "").Trim();
                Debug.Log("[OauthUI] User Info: " + identifier);
                SocketManager.singleton.InitSocketConnection();
                yield return new WaitForSeconds(1);
                SocketManager.singleton.RequestOauth(identifier);
            }
            else
            {
                string identifier = "google" + request.downloadHandler.text.Split(',')[0].Split(':')[1].Replace("\"", "").Trim();
                Debug.Log("[OauthUI] User Info: " + identifier);
                SocketManager.singleton.InitSocketConnection();
                yield return new WaitForSeconds(1);
                SocketManager.singleton.RequestOauth(identifier);
            }
            // Debug.Log("[OauthUI] User Info: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("[OauthUI] Error fetching user info: " + request.error);
        }
    }

    // 토큰 파싱
    private string ParseAccessToken(string jsonResponse)
    {
        try
        {
            TokenResponse tokenResponse = JsonUtility.FromJson<TokenResponse>(jsonResponse);
            return tokenResponse.access_token;
        }
        catch (Exception ex)
        {
            Debug.LogError("[OauthUI] Error parsing access token: " + ex.Message);
            return null;
        }
    }

    // Token Response Class
    [System.Serializable]
    private class TokenResponse
    {
        public string access_token;
        public string token_type;
        public int expires_in;
        public string refresh_token;
    }

    private void Update()
    {
        if (listenerQueue.Count > 0)
        {
            string[] data = listenerQueue.Dequeue().ToString().Split(' ');
            if (data[0].Equals("ssafy"))
            {
                ExchangeSSAFYCodeForToken(data[1]);
            }
            else
            {
                ExchangeGoogleCodeForToken(data[1]);
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (httpListener != null && httpListener.IsListening)
        {
            httpListener.Stop();
            httpListener.Close();
        }
    }
}