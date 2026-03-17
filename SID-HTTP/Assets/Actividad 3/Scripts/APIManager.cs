using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class APIManager : MonoBehaviour
{
    [Header("API")]
    private string baseURL = "https://sid-restapi.onrender.com";
    private string token;
    private string username;

    [Header("Panels")]
    public GameObject panelLogin;
    public GameObject panelRegister;
    public GameObject panelGame;

    [Header("Login Inputs")]
    public TMP_InputField loginUsername;
    public TMP_InputField loginPassword;

    [Header("Register Inputs")]
    public TMP_InputField registerUsername;
    public TMP_InputField registerPassword;

    [Header("Game UI")]
    public TMP_Text usernameText;
    public TMP_Text scoreText;

    [Header("Leaderboard")]
    public Transform leaderboardContainer;
    public GameObject leaderboardItemPrefab;

    int score = 0;

    void Start()
    {
        if (PlayerPrefs.HasKey("token"))
        {
            token = PlayerPrefs.GetString("token");
            username = PlayerPrefs.GetString("username");
            usernameText.text = username;
            MostrarJuego();
            StartCoroutine(GetLeaderboard());
        }
        else
        {
            MostrarLogin();
        }
    }

    // --------------------
    // CAMBIO DE PANELES
    // --------------------

    public void MostrarLogin()
    {
        panelLogin.SetActive(true);
        panelRegister.SetActive(false);
        panelGame.SetActive(false);
    }

    public void MostrarRegistro()
    {
        panelLogin.SetActive(false);
        panelRegister.SetActive(true);
        panelGame.SetActive(false);
    }

    public void MostrarJuego()
    {
        panelLogin.SetActive(false);
        panelRegister.SetActive(false);
        panelGame.SetActive(true);
    }

    // --------------------
    // REGISTER
    // --------------------

    public void RegisterButton()
    {
        StartCoroutine(Register(registerUsername.text, registerPassword.text));

        Debug.Log("Intentando login con: " + loginUsername.text + " " + loginPassword.text);
    }

    IEnumerator Register(string username, string password)
    {
        string url = baseURL + "/api/usuarios";

        UserData user = new UserData { username = username, password = password};
        string json = JsonUtility.ToJson(user);

        UnityWebRequest req = UnityWebRequest.Post(url, json, "application/json");
        Debug.Log("Url: " + url);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Registro exitoso");
            MostrarLogin();
        }
        else
        {
            Debug.LogError("Login failed" + req.error);
        }
    }

    // --------------------
    // LOGIN
    // --------------------

    public void LoginButton()
    {
        StartCoroutine(Login(loginUsername.text, loginPassword.text));
        Debug.Log("Intentando login con: " + loginUsername.text + " " + loginPassword.text);
    }

    IEnumerator Login(string username, string password)
    {
        string url = baseURL + "/api/auth/login";
        UserData user = new UserData { username = username, password = password };
        string json = JsonUtility.ToJson(user);

        UnityWebRequest req = UnityWebRequest.Post(url, json, "application/json");
        Debug.Log("Url: " + url);


        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);
            Debug.Log("Login exitoso. Token: " + response.token);
            
            token = response.token;
            username = response.usuario.username;

            PlayerPrefs.SetString("token", token);
            PlayerPrefs.SetString("username", username);

            usernameText.text = username;

            MostrarJuego();

            StartCoroutine(GetLeaderboard());
        }
        else
        {
            Debug.LogError("Login failed" + req.error);
        }
    }

    // --------------------
    // CLICKER SCORE
    // --------------------

    public void Click()
    {
        score++;

        scoreText.text = "Score: " + score;
    }

    // --------------------
    // ENVIAR SCORE
    // --------------------

    public void SendScore()
    {
        StartCoroutine(UpdateScore(score));
    }

    IEnumerator UpdateScore(int score)
    {
        string url = baseURL + "/score";

        string json = "{\"score\":" + score + "}";

        UnityWebRequest req = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + token);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Score actualizado");
            StartCoroutine(GetLeaderboard());
        }
        else
        {
            Debug.LogError(req.downloadHandler.text);
        }
    }

    // --------------------
    // LEADERBOARD
    // --------------------

    IEnumerator GetLeaderboard()
    {
        string url = baseURL + "/scores";

        UnityWebRequest req = UnityWebRequest.Get(url);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            ScoreList scores = JsonUtility.FromJson<ScoreList>(req.downloadHandler.text);

            foreach (Transform child in leaderboardContainer)
                Destroy(child.gameObject);

            foreach (ScoreEntry s in scores.scores)
            {
                GameObject item = Instantiate(leaderboardItemPrefab, leaderboardContainer);

                item.transform.Find("Username").GetComponent<TMP_Text>().text = s.username;
                item.transform.Find("Score").GetComponent<TMP_Text>().text = s.score.ToString();
            }
        }
    }

    // --------------------
    // LOGOUT
    // --------------------

    public void Logout()
    {
        PlayerPrefs.DeleteKey("token");

        score = 0;

        scoreText.text = "Score: 0";

        MostrarLogin();
    }
}

[System.Serializable]
public class UserData
{
    public string username;
    public string password;
}
[System.Serializable]
public class User
{
    public string _id;
    public string username;
}

[System.Serializable]
public class LoginResponse
{
    public User usuario;
    public string token;
}

[System.Serializable]
public class ScoreEntry
{
    public string username;
    public int score;
}

[System.Serializable]
public class ScoreList
{
    public ScoreEntry[] scores;
}