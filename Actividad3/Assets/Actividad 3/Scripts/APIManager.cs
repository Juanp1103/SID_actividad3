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

    private User currentUser;

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

    int score;

    void Start()
    {       
        token = PlayerPrefs.GetString("token", null);
        username = PlayerPrefs.GetString("username", null);
        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username))
        {
            usernameText.text = username;
            MostrarJuego();

            StartCoroutine(GetUsuario());
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
    }

    IEnumerator Register(string username, string password)
    {
        string url = baseURL + "/api/usuarios";

        Userauth user = new Userauth { username = username, password = password};
        string json = JsonUtility.ToJson(user);

        UnityWebRequest req = UnityWebRequest.Post(url, json, "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Registro exitoso");
            MostrarLogin();
        }
        else
        {
            Debug.LogError("Register failed " + req.error);
        }
    }

    // --------------------
    // LOGIN
    // --------------------

    public void LoginButton()
    {
        StartCoroutine(Login(loginUsername.text, loginPassword.text));
    }

    IEnumerator Login(string Username, string password)
    {
        string url = baseURL + "/api/auth/login";

        Userauth user = new Userauth { username = Username, password = password};
        string json = JsonUtility.ToJson(user);

        UnityWebRequest req = UnityWebRequest.Post(url, json, "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);

            token = response.token;
            username = response.usuario.username;
            PlayerPrefs.SetString("token", token);
            PlayerPrefs.SetString("username", Username);

            usernameText.text = Username;

            MostrarJuego();

            StartCoroutine(GetUsuario());
            StartCoroutine(GetLeaderboard());
        }
        else
        {
            Debug.LogError("Login failed " + req.error);
        }
    }

    // --------------------
    // GET USUARIO
    // --------------------

    IEnumerator GetUsuario()
    {
        string url = baseURL + "/api/usuarios/";

        UnityWebRequest req = UnityWebRequest.Get(url + username);
        req.SetRequestHeader("x-token", token);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            currentUser = JsonUtility.FromJson<User>(req.downloadHandler.text);

            score = currentUser.data.score;

            scoreText.text = "Score: " + score;
            Debug.Log("Getusuario" + req.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error obteniendo usuario: " + req.error);
        }
    }

    // --------------------
    // CLICKER SCORE
    // --------------------

    public void Click()
    {
        score+=1000;
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
        string url = baseURL + "/api/usuarios/";
        currentUser.username = username;
        currentUser.data.score = score;
        string json = JsonUtility.ToJson(currentUser);

        //UnityWebRequest req = UnityWebRequest.Put(url, json);
        //req.method = "PATCH";
        //Debug.Log("jspn score" + json );
        //req.SetRequestHeader("x-token", token);

        UnityWebRequest req = new UnityWebRequest(url, "PATCH");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("x-token", token);

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
            string url = baseURL + "/api/usuarios";

            UnityWebRequest req = UnityWebRequest.Get(url);
            req.SetRequestHeader("x-token", token);

        yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                UserList users = JsonUtility.FromJson<UserList>(req.downloadHandler.text);

                foreach (Transform child in leaderboardContainer)
                    Destroy(child.gameObject);

                System.Array.Sort(users.usuarios, (a, b) => b.data.score.CompareTo(a.data.score));

                int top = Mathf.Min(3, users.usuarios.Length);

                for (int i = 0; i < top; i++)
                {
                    User u = users.usuarios[i];

                    GameObject item = Instantiate(leaderboardItemPrefab, leaderboardContainer);

                    item.transform.Find("Username").GetComponent<TMP_Text>().text = (i + 1) + ". " + u.username;
                    item.transform.Find("Score").GetComponent<TMP_Text>().text = u.data.score.ToString();
                }
            }
            else
            {
                Debug.LogError(req.downloadHandler.text);
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

// --------------------
// CLASES
// --------------------

[System.Serializable]
public class Userauth
{
    public string username;
    public string password;
}

[System.Serializable]
public class User
{
    public string username;
    public UserData data;
}

[System.Serializable]
public class UserData
{
    public int score;
}

[System.Serializable]
public class LoginResponse
{
    public User usuario;
    public string token;
}

[System.Serializable]
public class UserList
{
    public User[] usuarios;
}