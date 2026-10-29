using Stride.Engine;
using Stride.Audio;

namespace DiscFaithful
{
    /// <summary>
    /// Wrapper audio – assegna i Sound estratti dall'ADF (launch, impact, …).
    /// </summary>
    public class AudioManager : StartupScript
    {
        public Sound Launch;
        public Sound Impact;
        public Sound TileDestroy;
        public Sound HitPlayer;
        public Sound Death;
        public Sound Victory;
        public Sound Gong;
        public Sound Block;

        private SoundInstance _launch;
        private SoundInstance _impact;
        private SoundInstance _tileDestroy;
        private SoundInstance _hitPlayer;
        private SoundInstance _death;
        private SoundInstance _victory;
        private SoundInstance _gong;
        private SoundInstance _block;

        public override void Start()
        {
            _launch = Launch?.CreateInstance();
            _impact = Impact?.CreateInstance();
            _tileDestroy = TileDestroy?.CreateInstance();
            _hitPlayer = HitPlayer?.CreateInstance();
            _death = Death?.CreateInstance();
            _victory = Victory?.CreateInstance();
            _gong = Gong?.CreateInstance();
            _block = Block?.CreateInstance();
        }

        public void PlayLaunch() => _launch?.Play();
        public void PlayImpact() => _impact?.Play();
        public void PlayTileDestroy() => _tileDestroy?.Play();
        public void PlayHitPlayer() => _hitPlayer?.Play();
        public void PlayDeath() => _death?.Play();
        public void PlayVictory() => _victory?.Play();
        public void PlayGong() => _gong?.Play();
        public void PlayBlock() => _block?.Play();
    }
}
