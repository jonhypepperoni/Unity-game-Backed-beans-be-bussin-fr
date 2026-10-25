using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Alvo")]
    public Transform alvo;             
    public Vector3 offset = new Vector3(0, 3, -6);

    [Header("Rotação")]
    public float sensibilidade = 3f;
    public float limiteVerticalMin = -70f;
    public float limiteVerticalMax = 90f;

    [Header("Colisão")]
    public LayerMask camadasColisao = ~0;  
    public float raioCamera = 0.3f;        
    public float distanciaMinima = 0.5f;    

    private float rotacaoX = 0f;
    private float rotacaoY = 0f;

    void Start()
    {
        // Vai esconder e bloquear o rato
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        rotacaoY = transform.eulerAngles.y;
    }

void LateUpdate()
{
    if (alvo == null) return;

    float mouseX = Input.GetAxis("Mouse X") * sensibilidade;
    float mouseY = Input.GetAxis("Mouse Y") * sensibilidade;

    rotacaoY += mouseX;
    rotacaoX -= mouseY;
    rotacaoX = Mathf.Clamp(rotacaoX, limiteVerticalMin, limiteVerticalMax);

    Quaternion rotacao = Quaternion.Euler(rotacaoX, rotacaoY, 0f);

    Vector3 pontoFoco = alvo.position + Vector3.up * 1.5f;
    Vector3 posicaoDesejada = alvo.position + rotacao * offset;

    Vector3 direcao = posicaoDesejada - pontoFoco;
    float distanciaDesejada = direcao.magnitude;

    RaycastHit hit;
    bool acertou = Physics.SphereCast(pontoFoco, raioCamera, direcao.normalized, out hit, distanciaDesejada, camadasColisao);

    // DEBUG — remove depois de resolver
    Debug.DrawLine(pontoFoco, posicaoDesejada, acertou ? Color.red : Color.green);
    if (acertou) Debug.Log("Colidiu com: " + hit.collider.name + " | layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));

    if (acertou)
    {
        float distanciaAjustada = Mathf.Max(hit.distance, distanciaMinima);
        posicaoDesejada = pontoFoco + direcao.normalized * distanciaAjustada;
    }

    transform.position = posicaoDesejada;
    transform.LookAt(pontoFoco);
}
}