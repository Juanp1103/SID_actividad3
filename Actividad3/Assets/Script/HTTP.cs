using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Http : MonoBehaviour
{
    [SerializeField] private string jugadoresURL;
    [SerializeField] private Transform cartasContainer;
    [SerializeField] private GameObject cartaPrefab;
    [SerializeField] private TextMeshProUGUI nombreJugadorText;

    private JugadorLista listaJugadores;
    private int indiceJugador = 0;

    void Start()
    {
        StartCoroutine(GetJugadores());
    }

    IEnumerator GetJugadores()
    {
        UnityWebRequest www = UnityWebRequest.Get(jugadoresURL);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            listaJugadores = JsonUtility.FromJson<JugadorLista>(www.downloadHandler.text);

            CargarJugador();
        }
    }
    void CargarJugador()
    {
        Jugador jugadorActual = listaJugadores.jugadores[indiceJugador];

        nombreJugadorText.text = jugadorActual.nombre;

        string ids = string.Join(",", jugadorActual.cartas);

        StartCoroutine(GetCharacters(ids));
    }
    IEnumerator GetCharacters(string ids)
    {
        string url = "https://rickandmortyapi.com/api/character/" + ids;

        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Character[] characters = JsonHelper.FromJson<Character>(www.downloadHandler.text);

            MostrarCartas(characters);
        }
    }
    void LimpiarCartas()
    {
        foreach (Transform child in cartasContainer)
        {
            Destroy(child.gameObject);
        }
    }
    void MostrarCartas(Character[] characters)
    {
        LimpiarCartas();

        foreach (Character c in characters)
        {
            GameObject carta = Instantiate(cartaPrefab, cartasContainer);
            carta.transform.Find("Nombre").GetComponent<TMP_Text>().text = c.name;
            carta.transform.Find("Especie").GetComponent<TMP_Text>().text = c.species;

            StartCoroutine(DescargarImagen(c.image, carta));
        }
    }
    IEnumerator DescargarImagen(string url, GameObject carta)
    {
        UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
            carta.transform.Find("Imagen").GetComponent<RawImage>().texture = texture;
        }
    }
    public void CambiarJugador()
    {
        indiceJugador++;

        if (indiceJugador >= listaJugadores.jugadores.Length)
            indiceJugador = 0;

        CargarJugador();
    }
}

[System.Serializable]
class Character
{
    public int id;
    public string name;
    public string species;
    public string image;

}

[System.Serializable]
public class Jugador
{
    public int id;
    public string nombre;
    public int[] cartas;
}

[System.Serializable]
public class JugadorLista
{
    public Jugador[] jugadores;
}

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}


