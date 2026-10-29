# Disc / Frisbee

## File presente (locale)

| File | Formato | Note |
|------|---------|------|
| `frisbee.blend` | Blender (.blend) | ~352 KB – modello fornito dall'utente |

**Percorso locale:**  
`/home/workdir/artifacts/DiscFaithful/assets/disc/frisbee.blend`

> Il file binario va caricato manualmente su GitHub (Add file → Upload files nella cartella `assets/disc/`).

## Come usarlo in Stride

1. Apri `frisbee.blend` in **Blender**
2. Esporta in **glTF 2.0 (.glb)** (consigliato) oppure FBX
3. Copia il `.glb` nella cartella Assets del progetto Stride
4. Importa come Model
5. Assegnalo al prefab Disc (ModelComponent / VisualModel)

### Consigli esportazione Blender → Stride
- Apply All Transforms (Ctrl+A)
- Origin al centro del disco
- Diametro target ≈ 0.7 unità
- Materiale emissive (arancione o ciano) per buona leggibilità
- Trail: gestiscilo con ParticleSystem in Stride (più flessibile)

## Alternativa rapida (placeholder)
Cylinder nativo di Stride:
- Scale XZ = 0.7, Y = 0.08
- Materiale emissive
