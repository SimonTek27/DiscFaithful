using System.Collections.Generic;
using Stride.Audio;
using Stride.Engine;

namespace DiscFaithful
{
    /// <summary>
    /// Gestione audio semplice (mappa i sample originali: LAUNCH, IMPACT, DESDALLE, MORT, VICTOIRE…).
    /// </summary>
    public class AudioManager : StartupScript
    {
        public static AudioManager Instance { get; private set; }

        public Sound SndLaunch;
        public Sound SndImpact;
        public Sound SndDesDalle;
        public Sound SndHitPlayer;
        public Sound SndMort;
        public Sound SndVictoire;
        public Sound SndGong;
        public Sound SndParade;

        private readonly Dictionary<string, Sound> map = new();

        public override void Start()
        {
            Instance = this;

            Register("Launch", SndLaunch);
            Register("Impact", SndImpact);
            Register("DesDalle", SndDesDalle);
            Register("HitPlayer", SndHitPlayer);
            Register("Mort", SndMort);
            Register("Victoire", SndVictoire);
            Register("Gong", SndGong);
            Register("Parade", SndParade);
        }

        private void Register(string key, Sound snd)
        {
            if (snd != null)
                map[key] = snd;
        }

        public void Play(string key, float volume = 1f)
        {
            if (!map.TryGetValue(key, out var snd) || snd == null)
                return;

            var instance = snd.CreateInstance();
            instance.Volume = volume;
            instance.Play();
        }

        public void PlayLaunch()    => Play("Launch");
        public void PlayImpact()    => Play("Impact");
        public void PlayDesDalle()  => Play("DesDalle");
        public void PlayHitPlayer() => Play("HitPlayer");
        public void PlayMort()      => Play("Mort");
        public void PlayVictoire()  => Play("Victoire");
        public void PlayGong()      => Play("Gong");
        public void PlayParade()    => Play("Parade");
    }
}
