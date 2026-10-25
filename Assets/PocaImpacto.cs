using UnityEngine;

public class PocaImpacto : MonoBehaviour
{
    [Header("Poça")]
    public GameObject pocaPrefab;        // prefab da poça a inserir no unity
    public float duracaoPoca = 2f;
    public float tamanhoMax = 0.3f;
    public float velocidadeExpansao = 4f;

    private CharacterController characterController;
    private bool estavaNoCHao;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        bool noChao = characterController.isGrounded;

        

        estavaNoCHao = noChao;
    }

    public void CriarPoca()
    {
        if (pocaPrefab == null) return;

        Vector3 posicao = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);
        GameObject poca = Instantiate(pocaPrefab, posicao, Quaternion.Euler(90f, 0f, 0f));
        StartCoroutine(AnimarPoca(poca));
    }

    private System.Collections.IEnumerator AnimarPoca(GameObject poca)
    {
        Renderer r = poca.GetComponent<Renderer>();
        float tempo = 0f;

        // Expansão
        while (tempo < 0.3f)
        {
            tempo += Time.deltaTime;
            float escala = Mathf.Lerp(0f, tamanhoMax, tempo / 0.3f * velocidadeExpansao);
            escala = Mathf.Min(escala, tamanhoMax);
            poca.transform.localScale = new Vector3(escala, escala, escala);
            yield return null;
        }

        poca.transform.localScale = pocaPrefab.transform.localScale * tamanhoMax;

        // Espera
        yield return new WaitForSeconds(duracaoPoca);

        // Fade out
        tempo = 0f;
        Color cor = r.material.color;
        while (tempo < 0.5f)
        {
            tempo += Time.deltaTime;
            cor.a = Mathf.Lerp(1f, 0f, tempo / 0.5f);
            r.material.color = cor;
            yield return null;
        }

        Destroy(poca);
    }
}