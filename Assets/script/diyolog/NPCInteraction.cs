using TMPro;
using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    public string[] dialogLines;
    public TextMeshProUGUI dialogText;
    public GameObject[] storedElements;
    public GameObject diyolog;
    public GameObject player;
    public SC_FPSController fpsController;
    public Camera currentCamera;

    private int currentLine = 0;
    private bool isDialogActive = false;

    // FOV ayarlarý
    private float originalFOV = 60f;
    public float zoomFOV = 45f;
    public float fovLerpSpeed = 20f;
    private bool isZoomFOVActive = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        fpsController = player.GetComponent<SC_FPSController>();

        if (currentCamera == null)
            currentCamera = Camera.main;

        originalFOV = currentCamera.fieldOfView;
    }

    void Update()
    {
        if (isDialogActive && Input.GetKeyDown(KeyCode.Space))
        {
            NextLine();
        }

        // FOV geçiþi
        if (isZoomFOVActive && currentCamera.fieldOfView > zoomFOV)
        {
            currentCamera.fieldOfView = Mathf.Lerp(currentCamera.fieldOfView, zoomFOV, Time.deltaTime * fovLerpSpeed);
        }
        else if (!isZoomFOVActive && currentCamera.fieldOfView < originalFOV)
        {
            currentCamera.fieldOfView = Mathf.Lerp(currentCamera.fieldOfView, originalFOV, Time.deltaTime * fovLerpSpeed);
        }
    }

    public void StartDialog()
    {
        currentLine = 0;
        isDialogActive = true;
        diyolog.SetActive(true);
        dialogText.text = dialogLines[currentLine];

        fpsController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (GameObject obj in storedElements)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        isZoomFOVActive = true;
    }

    void NextLine()
    {
        currentLine++;
        if (currentLine < dialogLines.Length)
        {
            dialogText.text = dialogLines[currentLine];
        }
        else
        {
            EndDialog();
        }
    }

    void EndDialog()
    {
        isDialogActive = false;
        fpsController.enabled = true;
        diyolog.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        foreach (GameObject obj in storedElements)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        isZoomFOVActive = false;
    }
}
