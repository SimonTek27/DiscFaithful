# Audio – Sample estratti dall’ADF originale

## File estratti (presenti in locale)

| File WAV          | Origine ADF     | Uso in gioco           | Campo AudioManager |
|-------------------|-----------------|------------------------|--------------------|
| LAUNCH.wav        | LAUNCH.SPL      | Lancio disco           | SndLaunch          |
| IMPACT.wav        | IMPACT.SPL      | Impatto / rimbalzo     | SndImpact          |
| DESDALLE.wav      | DESDALLE.SPL    | Distruzione piastrella | SndDesDalle        |
| MORT.wav          | MORT.SPL        | Morte / caduta         | SndMort            |
| VICTOIRE.wav      | VICTOIRE.SPL    | Vittoria               | SndVictoire        |
| GONG.wav          | GONG.SPL        | Inizio round           | SndGong            |
| PARADE.wav        | PARADE.SPL      | Parata                 | SndParade          |
| CHUTE.wav         | CHUTE.SPL       | Caduta                 | extra              |
| TOUCHDEF.wav      | TOUCHDEF.SPL    | Tocco difesa           | extra              |
| VITRE15K.wav      | VITRE15K.SPL    | Vetro / impatto        | extra              |
| DIC13.wav         | DIC13.SPL       | extra                  | extra              |

**Percorso locale:**  
`/home/workdir/artifacts/DiscFaithful/assets/audio/`

## Formato
- 8-bit mono, 15500 Hz
- Convertiti da sample Amiga signed → unsigned WAV

## Nota
Alcuni sample condividevano il settore di partenza sull’ADF. Se un suono sembra tagliato o con rumore, regola start offset o sample rate in un editor audio.

## Uso in Stride
Importa i `.wav` come Sound e assegnali ai campi dell’`AudioManager`.

I file binari (`.wav` / `.SPL`) vanno caricati manualmente su GitHub con Upload files.
