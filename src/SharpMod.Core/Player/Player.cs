using System;
using SharpMod;
using SharpMod.UniTracker;
using SharpMod.Song;
using SharpMod.Mixer;

namespace SharpMod.Player
{
    internal delegate void UpdateUIHandler();
    public delegate ActionsEnum GetUIEventHandler();
    internal delegate void CurrentModEndHandler();

    /// <summary>
    /// The actual modplaying routines
    /// </summary>
    public class SharpModPlayer
    {
        public event GetUIEventHandler OnGetUIActions;
        internal event UpdateUIHandler OnUpdateUI;
        internal event CurrentModEndHandler OnCurrentModEnd;

        public ChannelsMixer _mixer { get; set; }

        private UniTrk _uniTrack;
        //private DriverContainer _driver;

        public float SpeedConstant { get; set; }
        public bool Quit { get; set; }
        public int PauseFlag { get; set; }
        public bool PlayCurrent { get; set; }
        public ActionsEnum UIResult { get; set; }

        /// <summary>
        /// this modfile is being played
        /// </summary>
        public SongModule CurrentUniMod { get; set; }

        /// <summary>
        ///  patternloop position
        /// </summary>
        public short PatternLoopPosition { get; set; }

        /// <summary>
        /// times to loop 
        /// </summary>
        public short RepeatCounter { get; set; }

        /// <summary>
        /// Tick Counter
        /// </summary>
        public short TickCounter { get; set; }

        /// <summary>
        /// position where to start a new pattern
        /// </summary>
        public short PatternBreakPosition { get; set; }

        /// <summary>
        /// Pattern Delay Counter
        /// </summary>
        public short PatternDelayCounter { get; set; }

        /// <summary>
        /// Pattern Delay Counter 2
        /// </summary>
        public short SecondPatternDelayCounter { get; set; }

        /// <summary>
        /// number of rows on current pattern
        /// </summary>
        public int numrow { get; set; }

        /// <summary>
        /// flag to indicate a position jump is needed...
        /// changed since 1.00: now also indicates the
        /// direction the position has to jump to:
        ///
        /// 0: Don't do anything
        /// 1: Jump back 1 position
        /// 2: Restart on current position
        /// 3: Jump forward 1 position
        /// </summary>
        public short posjmp { get; set; }

        /// <summary>
        /// forbidflag
        /// Set forbid to 1 when you want to modify any of the mp_sngpos, mp_patpos etc.
        /// variables and clear it when you're done. This prevents getting strange
        /// results due to intermediate interrupts.       
        /// </summary>
        public bool forbid { get; set; }

        protected internal int isfirst { get; set; }

        public ChannelMemory[] mp_audio { get; set; }//[32];    /* max 32 channels */

        /// <summary>
        /// beats-per-minute speed
        /// </summary>
        public short mp_bpm { get; set; }

        /// <summary>
        /// current row number (0-255)
        /// </summary>
        private short _mp_patpos;
        public short mp_patpos
        {
            get { return _mp_patpos; }
            set
            {
                _mp_patpos = value;
                if (OnUpdateUI != null)
                    OnUpdateUI();
            }
        }

        /// <summary>
        /// current song position
        /// </summary>
        public short mp_sngpos { get; set; }

        /// <summary>
        /// current songspeed
        /// </summary>
        public short mp_sngspd { get; set; }

        /// <summary>
        /// channel it's working on 
        /// </summary>
        public short mp_channel { get; set; }

        /// <summary>
        ///  extended speed flag, default enabled
        /// </summary>
        public bool mp_extspd { get; set; }

        /// <summary>
        /// panning flag, default enabled
        /// </summary>
        public bool mp_panning { get; set; }

        /// <summary>
        /// loop module ?
        /// </summary>
        public bool mp_loop { get; set; }

        /// <summary>
        /// song volume (0-100) (or user volume)
        /// </summary>
        public short mp_volume { get; set; }

        /// <summary>
        ///  global volume
        /// </summary>
        protected internal sbyte globalvolume { get; set; }
        protected internal short globalslide { get; set; }

        /// <summary>
        /// current ChannelMemory it's working on
        /// </summary>
        public ChannelMemory a { get; set; }

        public float old_bpm { get; set; }

        internal static short[] toshortarray(int[] intarray)
        {
            short[] shortarray = new short[intarray.Length];
            int i;
            for (i = 0; i < intarray.Length; i++)
                shortarray[i] = (short)intarray[i];
            return shortarray;
        }

        protected internal static short[] mytab = new short[] { (short)(1712 * 16), (short)(1616 * 16), (short)(1524 * 16), (short)(1440 * 16), (short)(1356 * 16), (short)(1280 * 16), (short)(1208 * 16), (short)(1140 * 16), (short)(1076 * 16), (short)(1016 * 16), (short)(960 * 16), (short)(907 * 16) };

        protected internal short[] VibratoTable = new short[]{ 
            0, 24, 49, 74, 97, 120, 141, 161,
            180, 197, 212, 224, 235, 244, 250, 253,
            255, 253, 250, 244, 235, 224, 212, 197,
            180, 161, 141, 120, 97, 74, 49, 24 };

        /// <summary>
        /// linear periods to frequency translation table
        /// </summary>
        protected internal static readonly int[] lintab = new int[]{16726, 16741, 16756, 16771, 16786, 16801, 16816, 16832, 16847, 16862, 16877, 16892, 16908, 16923, 16938, 16953, 16969, 16984, 16999, 17015, 17030, 17046, 17061, 17076, 17092, 17107, 17123, 17138, 17154, 17169, 17185, 17200, 17216, 17231, 17247, 17262, 17278, 17293, 17309, 17325, 17340, 17356, 17372, 17387, 17403, 17419, 17435, 17450, 17466, 17482, 17498, 17513, 17529, 17545, 17561, 17577, 17593, 17608, 17624, 17640, 17656, 17672, 17688, 17704, 17720, 17736, 17752, 17768, 17784, 17800, 17816, 17832, 17848, 17865, 17881, 17897, 17913, 17929, 17945, 17962, 17978, 17994, 18010, 18027, 18043, 18059, 18075, 18092, 18108, 18124, 18141, 18157, 18174, 18190, 18206, 18223, 18239, 18256, 18272, 18289, 18305, 18322, 18338, 18355, 18372, 18388, 18405, 18421, 18438, 18455, 18471, 18488, 18505, 18521, 18538, 18555, 18572, 18588, 18605, 18622, 18639, 18656, 18672, 18689, 18706, 18723, 18740, 18757, 18774, 18791, 18808, 18825, 18842, 18859, 18876, 18893, 18910, 18927, 18944, 18961, 18978, 18995, 19013, 19030, 19047, 19064, 19081, 19099, 19116, 19133, 19150, 19168, 19185, 19202, 19220, 19237, 19254, 19272, 19289, 19306, 19324, 19341, 19359, 19376, 19394, 19411, 19429, 19446, 19464, 19482, 19499, 19517, 19534, 19552, 19570, 19587, 19605, 19623, 19640, 19658, 19676, 19694, 19711, 19729, 19747, 19765, 19783, 19801, 19819, 19836, 19854, 19872, 19890, 19908, 19926, 19944, 19962, 19980, 19998, 20016, 20034, 20052, 20071, 20089, 20107, 20125, 20143, 20161, 20179, 20198, 20216, 20234, 20252, 20271, 20289, 20307, 20326, 20344, 20362, 20381, 20399, 20418, 20436, 20455, 20473, 20492, 20510, 20529, 20547, 20566, 20584, 20603, 20621, 20640, 20659, 20677, 20696, 20715, 20733, 20752, 20771, 20790, 20808, 20827, 20846, 20865, 20884, 20902, 20921, 20940, 20959, 20978, 20997, 21016, 21035, 21054, 21073, 21092, 21111, 21130, 21149, 21168, 21187, 21206, 21226, 21245, 21264, 21283, 21302, 21322, 21341, 21360, 21379, 21399, 21418, 21437, 21457, 21476, 21496, 21515, 21534, 21554, 
			21573, 21593, 21612, 21632, 21651, 21671, 21690, 21710, 21730, 21749, 21769, 21789, 21808, 21828, 21848, 21867, 21887, 21907, 21927, 21946, 21966, 21986, 22006, 22026, 22046, 22066, 22086, 22105, 22125, 22145, 22165, 22185, 22205, 22226, 22246, 22266, 22286, 22306, 22326, 22346, 22366, 22387, 22407, 22427, 22447, 22468, 22488, 22508, 22528, 22549, 22569, 22590, 22610, 22630, 22651, 22671, 22692, 22712, 22733, 22753, 22774, 22794, 22815, 22836, 22856, 22877, 22897, 22918, 22939, 22960, 22980, 23001, 23022, 23043, 23063, 23084, 23105, 23126, 23147, 23168, 23189, 23210, 23230, 23251, 23272, 23293, 23315, 23336, 23357, 23378, 23399, 23420, 23441, 23462, 23483, 23505, 23526, 23547, 23568, 23590, 23611, 23632, 23654, 23675, 23696, 23718, 23739, 23761, 23782, 23804, 23825, 23847, 23868, 23890, 23911, 23933, 23954, 23976, 23998, 24019, 24041, 24063, 24084, 24106, 24128, 24150, 24172, 24193, 24215, 24237, 24259, 24281, 24303, 24325, 24347, 24369, 24391, 24413, 24435, 24457, 24479, 24501, 24523, 24545, 24567, 24590, 24612, 24634, 24656, 24679, 24701, 24723, 24746, 24768, 24790, 24813, 24835, 24857, 24880, 24902, 24925, 24947, 24970, 24992, 25015, 25038, 25060, 25083, 25105, 25128, 25151, 25174, 25196, 25219, 25242, 25265, 25287, 25310, 25333, 25356, 25379, 25402, 25425, 25448, 25471, 25494, 25517, 25540, 25563, 25586, 25609, 25632, 25655, 25678, 25702, 25725, 25748, 25771, 25795, 25818, 25841, 25864, 25888, 25911, 25935, 25958, 25981, 26005, 26028, 26052, 26075, 26099, 26123, 26146, 26170, 26193, 26217, 26241, 26264, 26288, 26312, 26336, 26359, 26383, 26407, 26431, 26455, 26479, 26502, 26526, 26550, 26574, 26598, 26622, 26646, 26670, 26695, 26719, 26743, 26767, 26791, 26815, 26839, 26864, 26888, 26912, 26937, 26961, 26985, 27010, 27034, 27058, 27083, 27107, 27132, 27156, 27181, 27205, 27230, 27254, 27279, 27304, 27328, 27353, 27378, 27402, 27427, 27452, 27477, 27502, 27526, 27551, 27576, 27601, 27626, 27651, 27676, 27701, 27726, 27751, 27776, 27801, 27826, 27851, 27876, 27902, 27927, 27952, 27977, 28003, 
			28028, 28053, 28078, 28104, 28129, 28155, 28180, 28205, 28231, 28256, 28282, 28307, 28333, 28359, 28384, 28410, 28435, 28461, 28487, 28513, 28538, 28564, 28590, 28616, 28642, 28667, 28693, 28719, 28745, 28771, 28797, 28823, 28849, 28875, 28901, 28927, 28953, 28980, 29006, 29032, 29058, 29084, 29111, 29137, 29163, 29190, 29216, 29242, 29269, 29295, 29322, 29348, 29375, 29401, 29428, 29454, 29481, 29507, 29534, 29561, 29587, 29614, 29641, 29668, 29694, 29721, 29748, 29775, 29802, 29829, 29856, 29883, 29910, 29937, 29964, 29991, 30018, 30045, 30072, 30099, 30126, 30154, 30181, 30208, 30235, 30263, 30290, 30317, 30345, 30372, 30400, 30427, 30454, 30482, 30509, 30537, 30565, 30592, 30620, 30647, 30675, 30703, 30731, 30758, 30786, 30814, 30842, 30870, 30897, 30925, 30953, 30981, 31009, 31037, 31065, 31093, 31121, 31149, 31178, 31206, 31234, 31262, 31290, 31319, 31347, 31375, 31403, 31432, 31460, 31489, 31517, 31546, 31574, 31602, 31631, 31660, 31688, 31717, 31745, 31774, 31803, 31832, 31860, 31889, 31918, 31947, 31975, 32004, 32033, 32062, 32091, 32120, 32149, 32178, 32207, 32236, 32265, 32295, 32324, 32353, 32382, 32411, 32441, 32470, 32499, 32529, 32558, 32587, 32617, 32646, 32676, 32705, 32735, 32764, 32794, 32823, 32853, 32883, 32912, 32942, 32972, 33002, 33031, 33061, 33091, 33121, 33151, 33181, 33211, 33241, 33271, 33301, 33331, 33361, 33391, 33421};

        protected internal const int LOGFAC = 2 * 16;

        protected internal static readonly short[] logtab = new short[]{
            (short) (LOGFAC * 907), (short) (LOGFAC * 900), (short) (LOGFAC * 894),
            (short) (LOGFAC * 887), (short) (LOGFAC * 881), (short) (LOGFAC * 875),
            (short) (LOGFAC * 868), (short) (LOGFAC * 862), (short) (LOGFAC * 856),
            (short) (LOGFAC * 850), (short) (LOGFAC * 844), (short) (LOGFAC * 838),
            (short) (LOGFAC * 832), (short) (LOGFAC * 826), (short) (LOGFAC * 820),
            (short) (LOGFAC * 814), (short) (LOGFAC * 808), (short) (LOGFAC * 802),
            (short) (LOGFAC * 796), (short) (LOGFAC * 791), (short) (LOGFAC * 785),
            (short) (LOGFAC * 779), (short) (LOGFAC * 774), (short) (LOGFAC * 768),
            (short) (LOGFAC * 762), (short) (LOGFAC * 757), (short) (LOGFAC * 752),
            (short) (LOGFAC * 746), (short) (LOGFAC * 741), (short) (LOGFAC * 736),
            (short) (LOGFAC * 730), (short) (LOGFAC * 725), (short) (LOGFAC * 720),
            (short) (LOGFAC * 715), (short) (LOGFAC * 709), (short) (LOGFAC * 704),
            (short) (LOGFAC * 699), (short) (LOGFAC * 694), (short) (LOGFAC * 689),
            (short) (LOGFAC * 684), (short) (LOGFAC * 678), (short) (LOGFAC * 675),
            (short) (LOGFAC * 670), (short) (LOGFAC * 665), (short) (LOGFAC * 660),
            (short) (LOGFAC * 655), (short) (LOGFAC * 651), (short) (LOGFAC * 646),
            (short) (LOGFAC * 640), (short) (LOGFAC * 636), (short) (LOGFAC * 632),
            (short) (LOGFAC * 628), (short) (LOGFAC * 623), (short) (LOGFAC * 619),
            (short) (LOGFAC * 614), (short) (LOGFAC * 610), (short) (LOGFAC * 604),
            (short) (LOGFAC * 601), (short) (LOGFAC * 597), (short) (LOGFAC * 592),
            (short) (LOGFAC * 588), (short) (LOGFAC * 584), (short) (LOGFAC * 580),
            (short) (LOGFAC * 575), (short) (LOGFAC * 570), (short) (LOGFAC * 567),
            (short) (LOGFAC * 563), (short) (LOGFAC * 559), (short) (LOGFAC * 555),
            (short) (LOGFAC * 551), (short) (LOGFAC * 547), (short) (LOGFAC * 543),
            (short) (LOGFAC * 538), (short) (LOGFAC * 535), (short) (LOGFAC * 532),
            (short) (LOGFAC * 528), (short) (LOGFAC * 524), (short) (LOGFAC * 520),
            (short) (LOGFAC * 516), (short) (LOGFAC * 513), (short) (LOGFAC * 508),
            (short) (LOGFAC * 505), (short) (LOGFAC * 502), (short) (LOGFAC * 498),
            (short) (LOGFAC * 494), (short) (LOGFAC * 491), (short) (LOGFAC * 487),
            (short) (LOGFAC * 484), (short) (LOGFAC * 480), (short) (LOGFAC * 477),
            (short) (LOGFAC * 474), (short) (LOGFAC * 470), (short) (LOGFAC * 467),
            (short) (LOGFAC * 463), (short) (LOGFAC * 460), (short) (LOGFAC * 457),
            (short) (LOGFAC * 453), (short) (LOGFAC * 450), (short) (LOGFAC * 447),
            (short) (LOGFAC * 443), (short) (LOGFAC * 440), (short) (LOGFAC * 437),
            (short) (LOGFAC * 434), (short) (LOGFAC * 431)};

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="uniTrack"></param>
        /// <param name="driver"></param>
        public SharpModPlayer(UniTrk uniTrack/*, DriverContainer driver*/)
        {
            _uniTrack = uniTrack;
            //_driver = driver;

            mp_extspd = true;
            mp_panning = true;
            mp_loop = false;
            mp_volume = 100;
            isfirst = 0;
            globalvolume = 64;
            globalslide = 0;

            mp_audio = new ChannelMemory[32];

            for (int i = 0; i < 32; i++)
                mp_audio[i] = new ChannelMemory();


            //memset(mp_audio, 0, sizeof(mp_audio));
            for (int i = 0; i < 32; i++)
            {
                mp_audio[i].FadeVol = mp_audio[i].Start = mp_audio[i].Period = mp_audio[i].C2spd = mp_audio[i].TmpPeriod = mp_audio[i].WantedPeriod = mp_audio[i].SlideSpeed = mp_audio[i].PortSpeed = mp_audio[i].SampleOffset = 0;

                mp_audio[i].Volume = (sbyte)(mp_audio[i].Transpose = (sbyte)(mp_audio[i].Retrig = (sbyte)(mp_audio[i].TmpVolume = (sbyte)(mp_audio[i].VibPos = (sbyte)(mp_audio[i].TrmPos = ((sbyte)0))))));

                mp_audio[i].SampleNumber = (short)(mp_audio[i].Handle = (short)(mp_audio[i].Panning = (short)(mp_audio[i].PanSlideSpd = (short)(mp_audio[i].Note = (short)(mp_audio[i].OwnPer = (short)(mp_audio[i].OwnVol = (short)(mp_audio[i].S3mTremor = (short)(mp_audio[i].S3mTrOnOff = (short)(mp_audio[i].S3mVolSlide = (short)(mp_audio[i].S3mRtgSpeed = (short)(mp_audio[i].S3mRtgSlide = (short)(mp_audio[i].Glissando = (short)(mp_audio[i].WaveControl = (short)(mp_audio[i].VibSpd = (short)(mp_audio[i].VibDepth = (short)(mp_audio[i].TrmSpd = (short)(mp_audio[i].TrmDepth = ((short)0))))))))))))))))));

                mp_audio[i].KeyOn = mp_audio[i].Kick = false;

                mp_audio[i].Instrument = null;
                mp_audio[i].Sample = null;
                mp_audio[i].Row = null;

                mp_audio[i].VolEnv.Flg = (short)(mp_audio[i].VolEnv.Pts = (short)(mp_audio[i].VolEnv.Sus = (short)(mp_audio[i].VolEnv.Beg = (short)(mp_audio[i].VolEnv.End = (short)(mp_audio[i].VolEnv.CurrentCounter = (short)(mp_audio[i].VolEnv.EnvIdxA = mp_audio[i].VolEnv.EnvIdxB))))));
                mp_audio[i].VolEnv.EnvPoints = null;

                mp_audio[i].PanEnv.Flg = (short)(mp_audio[i].PanEnv.Pts = (short)(mp_audio[i].PanEnv.Sus = (short)(mp_audio[i].PanEnv.Beg = (short)(mp_audio[i].PanEnv.End = (short)(mp_audio[i].PanEnv.CurrentCounter = (short)(mp_audio[i].PanEnv.EnvIdxA = mp_audio[i].PanEnv.EnvIdxB))))));
                mp_audio[i].PanEnv.EnvPoints = null;
            }

        }

        public static short Interpolate(short p, short p1, short p2, short v1, short v2)
        {
            short dp, dv, di;

            if (p1 == p2)
                return v1;

            dv = (short)(v2 - v1);
            dp = (short)(p2 - p1);
            di = (short)(p - p1);

            return (short)(v1 + ((int)(di * dv) / dp));
        }


        public static int getlinearperiod(short note, int fine)
        {
            return ((10 * 12 * 16 * 4) - ((int)note * 16 * 4) - (fine / 2) + 64);
        }


        public static int getlogperiod(short note, int fine)
        {
            short n, o;
            int p1, p2, i;

            n = (short)(note % 12);
            o = (short)(note / 12);
            i = (n << 3) + (fine >> 4); /* n*8 + fine/16 */

            p1 = logtab[i];
            p2 = logtab[i + 1];

            return (Interpolate((short)(fine / 16), (short)0, (short)15, (short)p1, (short)p2) >> o);
        }


        public static int getoldperiod(short note, int c2spd)
        {
            short n, o;
            int period;

            if (c2spd == 0)
                return 4242;/* <- prevent divide overflow.. (42 eheh) */

            n = (short)(note % 12);
            o = (short)(note / 12);
            period = (short)(((8363L * mytab[n]) >> o) / c2spd);
            return period;
        }



        public virtual int GetPeriod(short note, int c2spd)
        {
            if ((CurrentUniMod.Flags & UniModFlags.UF_XMPERIODS) != 0)
            {
                return ((CurrentUniMod.Flags & UniModFlags.UF_LINEAR) != 0) ? getlinearperiod(note, c2spd) : getlogperiod(note, c2spd);
            }
            return (getoldperiod(note, c2spd));
        }






        public virtual void DoVibrato()
        {
            short q;
            int temp = 0;

            q = (short)((a.VibPos >> 2) & 0x1f);

            switch (a.WaveControl & 3)
            {
                case 0:
                    temp = VibratoTable[q];
                    break;

                case 1:
                    q <<= 3;
                    if (a.VibPos < 0)
                        q = (short)(255 - q);
                    temp = q;
                    break;

                case 2:
                    temp = 255;
                    break;
            }

            temp *= a.VibDepth;
            temp >>= 7;
            temp <<= 2;

            if (a.VibPos >= 0)
                a.Period = a.TmpPeriod + temp;
            else
                a.Period = a.TmpPeriod - temp;

            /* do not update when vbtick==0 */
            if (TickCounter != 0)
                a.VibPos = (sbyte)(a.VibPos + a.VibSpd);

        }



        public virtual void DoTremolo()
        {
            short q;
            int temp = 0;

            q = (short)((a.TrmPos >> 2) & 0x1f);

            switch ((a.WaveControl >> 4) & 3)
            {
                case 0:
                    temp = VibratoTable[q];
                    break;

                case 1:
                    q <<= 3;
                    if (a.TrmPos < 0)
                        q = (short)(255 - q);
                    temp = q;
                    break;

                case 2:
                    temp = 255;
                    break;
            }

            temp *= a.TrmDepth;
            temp >>= 6;

            if (a.TrmPos >= 0)
            {
                a.Volume = (sbyte)(a.TmpVolume + temp);
                if (a.Volume > 64)
                    a.Volume = 64;
            }
            else
            {
                a.Volume = (sbyte)(a.TmpVolume - temp);
                if (a.Volume < 0)
                    a.Volume = 0;
            }

            /* do not update when vbtick==0 */
            if (TickCounter != 0)
                a.TrmPos = (sbyte)(a.TrmPos + a.TrmSpd);

        }





        public virtual void DoS3MVolSlide(short inf)
        {
            short lo, hi;

            inf &= 0xFF;

            if (inf != 0)
            {
                a.S3mVolSlide = inf;
            }
            inf = a.S3mVolSlide;

            lo = (short)(inf & 0xf);
            hi = (short)(inf >> 4);

            if (hi == 0)
            {
                a.TmpVolume = (sbyte)(a.TmpVolume - lo);
            }
            else if (lo == 0)
            {
                a.TmpVolume = (sbyte)(a.TmpVolume + hi);
            }
            else if (hi == 0xf)
            {
                if (TickCounter == 0)
                    a.TmpVolume = (sbyte)(a.TmpVolume - lo);
            }
            else if (lo == 0xf)
            {
                if (TickCounter == 0)
                    a.TmpVolume = (sbyte)(a.TmpVolume + hi);
            }

            if (a.TmpVolume < 0)
                a.TmpVolume = 0;
            if (a.TmpVolume > 64)
                a.TmpVolume = 64;
        }



        public virtual void DoXMVolSlide(short inf)
        {
            short lo, hi;

            inf &= 0xFF;

            if (inf != 0)
            {
                a.S3mVolSlide = inf;
            }
            inf = a.S3mVolSlide;

            if (TickCounter == 0)
                return;

            lo = (short)(inf & 0xf);
            hi = (short)(inf >> 4);

            if (hi == 0)
                a.TmpVolume = (sbyte)(a.TmpVolume - lo);
            else
                a.TmpVolume = (sbyte)(a.TmpVolume + hi);

            if (a.TmpVolume < 0)
                a.TmpVolume = 0;
            else if (a.TmpVolume > 64)
                a.TmpVolume = 64;
        }



        public virtual void DoXMGlobalSlide(short inf)
        {
            short lo, hi;

            inf &= 0xFF;

            if (inf != 0)
            {
                globalslide = inf;
            }
            inf = globalslide;

            if (TickCounter == 0)
                return;

            lo = (short)(inf & 0xf);
            hi = (short)(inf >> 4);

            if (hi == 0)
                globalvolume = (sbyte)(globalvolume - lo);
            else
                globalvolume = (sbyte)(globalvolume + hi);

            if (globalvolume < 0)
                globalvolume = 0;
            else if (globalvolume > 64)
                globalvolume = 64;
        }



        public virtual void DoXMPanSlide(short inf)
        {
            short lo, hi;
            short pan;

            inf &= 0xFF;

            if (inf != 0)
                a.PanSlideSpd = inf;
            else
                inf = a.PanSlideSpd;

            if (TickCounter == 0)
                return;

            lo = (short)(inf & 0xf);
            hi = (short)(inf >> 4);

            /* slide right has absolute priority: */
            if (hi != 0)
                lo = 0;

            pan = a.Panning;

            pan = (short)(pan - lo);
            pan = (short)(pan + hi);

            if (pan < 0)
                pan = 0;
            if (pan > 255)
                pan = 255;

            a.Panning = pan;
        }



        public virtual void DoS3MSlideDn(short inf)
        {
            short hi, lo;

            inf &= 0xFF;

            if (inf != 0)
                a.SlideSpeed = inf;
            else
                inf = (short)(a.SlideSpeed);

            hi = (short)(inf >> 4);
            lo = (short)(inf & 0xf);

            if (hi == 0xf)
            {
                if (TickCounter == 0)
                    a.TmpPeriod += lo << 2;
            }
            else if (hi == 0xe)
            {
                if (TickCounter == 0)
                    a.TmpPeriod += lo;
            }
            else
            {
                if (TickCounter != 0)
                    a.TmpPeriod += inf << 2;
            }
        }



        public virtual void DoS3MSlideUp(short inf)
        {
            short hi, lo;

            inf &= 0xFF;

            if (inf != 0)
                a.SlideSpeed = inf;
            else
                inf = (short)(a.SlideSpeed);

            hi = (short)(inf >> 4);
            lo = (short)(inf & 0xf);

            if (hi == 0xf)
            {
                if (TickCounter == 0)
                    a.TmpPeriod -= lo << 2;
            }
            else if (hi == 0xe)
            {
                if (TickCounter == 0)
                    a.TmpPeriod -= lo;
            }
            else
            {
                if (TickCounter != 0)
                    a.TmpPeriod -= inf << 2;
            }
        }



        



       


        


        
        public virtual void DoToneSlide()
        {
            int dist;

            if (TickCounter == 0)
            {
                a.TmpPeriod = a.Period;
                return;
            }

            // We have to slide a.period towards a.wantedperiod, so
            //compute the difference between those two values
            dist = a.Period - a.WantedPeriod;

            // or if portamentospeed is too big 
            if (dist == 0 || a.PortSpeed > System.Math.Abs(dist))
            {
                // make tmpperiod equal tperiod 
                a.Period = a.WantedPeriod;
            }
            // dist>0 ? 
            else if (dist > 0)
            {
                // then slide up 
                a.Period -= a.PortSpeed;
            }
            else
            {
                // dist<0 . slide down 
                a.Period += a.PortSpeed;
            }

            /*      if(a.glissando){
			
            If glissando is on, find the nearest
            halfnote to a.tmpperiod
			
            for(t=0;t<60;t++){
            if(a.tmpperiod>=npertab[a.finetune][t]) break;
            }
			
            a.period=npertab[a.finetune][t];
            }
            else*/
            a.TmpPeriod = a.Period;
        }

        /// <summary>
        /// Arpeggio
        /// Cycles between note, note+x halftones, note+y halftones. 
        /// Ex: S3M/IT: C-4 01 .. J37 (MOD/XM: C-4 01 .. J37) 
        /// This will play C-4, C-4+3 semitones and C-4+7 semitones (C-4, D#4 and G-4) 
        /// Note: if both x and y are zero, this command is ignored in MOD/XM. 
        /// In S3M/IT modules, J00 uses the previous value.
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffect0(short dat)
        {
            short note;

            dat &= 0xFF;
            note = a.Note;

            if (dat != 0)
            {
                switch (TickCounter % 3)
                {
                    case 1:
                        note = (short)(note + (dat >> 4));
                        break;

                    case 2:
                        note = (short)(note + (dat & 0xf));
                        break;
                }
                a.Period = GetPeriod((short)(note + a.Transpose), a.C2spd);
                a.OwnPer = 1;
            }
        }

        /// <summary>
        /// Portamento Up (MOD/XM: 1xy, S3M/IT: Fxy)
        /// This will slide up the pitch of the current note being played by the given speed. 
        /// In S3M/IT mode, FFx is a fine portamento up by x, and FEx is a extra-fine portamento up.
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffect1(short dat)
        {
            if (dat != 0)
                a.SlideSpeed = (int)dat << 2;
            if (TickCounter != 0)
                a.TmpPeriod -= a.SlideSpeed;

        }

        /// <summary>
        /// Portamento Down (MOD/XM: 2xy, S3M/IT: Exy)
        /// This will slide down the pitch of the current note being played by the given speed. 
        /// In S3M/IT mode, EFx is a fine portamento down by x, and EEx is a extra-fine portamento up.
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffect2(short dat)
        {
            if (dat != 0)
                a.SlideSpeed = (int)dat << 2;
            if (TickCounter != 0)
                a.TmpPeriod += a.SlideSpeed;
        }

        /// <summary>
        /// Tone-Portamento (MOD/XM: 3xy, S3M/IT: Gxy)
        /// This command is used together with a note, and will bend the current pitch at the given speed towards the specified note. 
        /// Example:
        /// C-4 01 .. ...
        /// F-4 .. .. G05 (bend the note up towards F-4)
        /// ... .. .. G00 (continue to slide up, until F-4 is reached)
        /// If the glissando command has been used before, the pitch will be rounded to the nearest halftone.
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffect3(short dat)
        {
            // temp XM fix
            a.Kick = false;

            if (dat != 0)
            {
                a.PortSpeed = dat;
                a.PortSpeed <<= 2;
            }
            DoToneSlide();
            a.OwnPer = 1;
        }

        /// <summary>
        /// Vibrato (MOD/XM: 4xy, S3M/IT: Hxy)
        /// Vibrato with speed x and depth y. 
        /// This command will oscillate the frequency of the current note with a sine wave. 
        /// (You can change the vibrato waveform to a triangle wave, a square wave, or a random table by using the E4x (MOD/XM) or S3x (S3M/IT) command)
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffect4(short dat)
        {
            if ((dat & 0x0f) != 0)
                a.VibDepth = (short)(dat & 0xf);
            if ((dat & 0xf0) != 0)
                a.VibSpd = (short)((dat & 0xf0) >> 2);
            DoVibrato();
            a.OwnPer = 1;
        }

        /// <summary>
        /// Tone-Portamento + Volume Slide (MOD/XM: 5xy, S3M/IT: Lxy)
        /// See also: Tone-Portamento, Volume Slide. 
        /// This command is equivalent to Tone-Portamento and Volume Slide. 
        /// (MOD/XM: 300 + Axy, S3M/IT: G00 + Dxy)
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffect5(short dat)
        {
            a.Kick = false;
            DoToneSlide();
            DoVolSlide(dat);
            a.OwnPer = 1;
        }

        /// <summary>
        /// Vibrato + Volume Slide (MOD/XM: 6xy, S3M/IT: Kxy)
        /// See also: Vibrato, Volume Slide. 
        /// This command is equivalent to Vibrato and Volume Slide. 
        /// (MOD/XM: 400 + Axy, S3M/IT: H00 + Dxy or U00 + Dxy)
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffect6(short dat)
        {
            DoVibrato();
            DoVolSlide(dat);
            a.OwnPer = 1;
        }

        /// <summary>
        /// Tremolo (MOD/XM: 7xy, S3M/IT: Rxy)
        /// Similar to the vibrato, but changes the volume instead of the pitch.
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffect7(short dat)
        {
            if ((dat & 0x0f) != 0)
                a.TrmDepth = (short)(dat & 0xf);
            if ((dat & 0xf0) != 0)
                a.TrmSpd = (short)((dat & 0xf0) >> 2);
            DoTremolo();
            a.OwnVol = 1;
        }

        /// <summary>
        /// Set Panning (MOD/XM: 8xx, S3M/IT: Xxy)
        /// This commands sets the pan position of the current channel. 
        /// In XM/IT, the value ranges from 00 (left) to FF (right). 
        /// In MOD/S3M, the value ranges from 00 (left) to 80 (right). 
        /// If the value is A4 (In MOD/S3M), the command sets the channel panning as Surround.
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffect8(short dat)
        {
            if (mp_panning)
            {
                a.Panning = dat;
                CurrentUniMod.Panning[mp_channel] = dat;
            }
        }

        /// <summary>
        /// Set Sample Offset (MOD/XM: 9xx, S3M/IT: Oxx)
        /// This command, when used together with a note, will start playing the sample at the position xx*256 
        /// (instead of position 0). If xx is 00 (900 or O00), the previous value will be used.
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffect9(short dat)
        {
            if (dat != 0)
                a.SampleOffset = (int)dat << 8; /* <- 0.43 fix.. */
            a.Start = a.SampleOffset;
            if (a.Start > a.Sample.Length)
                a.Start = a.Sample.Length;
        }

        /// <summary>
        /// Volume Slide (MOD/XM: Axy, S3M/IT: Dxy)
        /// This command will slide up or down the current volume:
        /// A0x will decrease the current volume by x on every tick.
        /// Ax0 will increase the current volume by x on every tick.
        /// Total slide amount is x * (current_speed-1)
        /// Special note for S3M/IT:
        /// AFx will do a fine volume down by x.
        /// AxF will do a fine volume up by x.
        /// For fine volume slides, the total slide amount is x (The current speed doesn't matter).
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoVolSlide(short dat) // DoPTEffect9
        {
            dat &= 0xFF;

            // do not update when vbtick==0 
            if (TickCounter == 0)
                return;

            // volume slide
            a.TmpVolume = (sbyte)(a.TmpVolume + (dat >> 4));
            a.TmpVolume = (sbyte)(a.TmpVolume - (dat & 0xf));
            if (a.TmpVolume < 0)
                a.TmpVolume = 0;
            if (a.TmpVolume > 64)
                a.TmpVolume = 64;
        }

        /// <summary>
        /// Position Jump (MOD/XM/S3M/IT: Bxy)
        /// This command will cause the player to jump to the pattern position xy (hex). 
        /// Ie: B00 will restart the song from the start. 
        /// If used together with a pattern break, you can also specify the starting row (by default, it will play from the start of the pattern). 
        /// Note that most players disable backward jumps in the song if looping mode isn't enabled, so that it is not possible to loop a song forever (pretty annoying in a playlist).
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffectB(short dat)
        {
           /* if (SecondPatternDelayCounter != 0)
                return;
            if (dat < mp_sngpos)
                // avoid eternal looping
                return;

            PatternBreakPosition = 0;
            mp_sngpos = (short)(dat - 1);
            posjmp = 3;*/

            if (TickCounter != 0 || SecondPatternDelayCounter != 0)
                return;

            /* Vincent Voois uses a nasty trick in "Universal Bolero" */
            if (dat == mp_sngpos && PatternBreakPosition == _mp_patpos)
                return;

            if (mp_loop && PatternBreakPosition == 0 &&
                (dat < mp_sngpos ||
                     (mp_sngpos == (CurrentUniMod.Positions.Count - 1 - 1) && PatternBreakPosition == 0)
                    ))
            {
                /* if we don't loop, better not to skip the end of the
                   pattern, after all... so:
                mod.patbrk=0; */
                posjmp = 3;
            }
            else
            {
                /* if we were fading, adjust... */
                /*if (mp_sngpos == (CurrentUniMod.Positions.Count - 1 - 1))
                    mp_volume = CurrentUniMod.vol initvolume > 128 ? 128 : mod.initvolume;*/
                mp_sngpos = dat;
                posjmp = 2;
                _mp_patpos = 0;
            }

        }

        /// <summary>
        /// Set Volume (MOD/XM: Cxx, S3M/IT: undefined)
        /// This command will set the current volume to xx (hex). 
        /// Note that the maximum value is 40 (hex). 
        /// It is better to use the volume column for volume effects, except in MOD songs, since the volume column isn't saved in the file.
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffectC(short dat)
        {
            if (TickCounter != 0)
                return;

            if (dat > 64)
                dat = 64;

            a.TmpVolume = (sbyte)dat;
        }

        /// <summary>
        /// Pattern Break (MOD/XM: Dxx, S3M/IT:Cxx)
        /// This command will stop playing the current pattern and will jump to the next one in the order list (pattern sequence). 
        /// You can also select the row where to start the next pattern. 
        /// Note that the specified row xx is in Hex (Ie D20 will jump to the 32nd row of the next pattern).
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffectD(short dat)
        {
            if (SecondPatternDelayCounter != 0)
                return;
            {
                int hi = (dat & 0xf0) >> 4;
                int lo = (dat & 0xf);
                PatternBreakPosition = (short)((hi * 10) + lo);
            }
            if (PatternBreakPosition > 64)
                PatternBreakPosition = 64; /* <- v0.42 fix */
            posjmp = 3;
        }

        /// <summary>
        /// Extended MOD Commands (MOD/XM: Exy, S3M/IT:undefined)
        /// Most of these can be mapped to on of the Sxy: Extended S3M Commands:
        /// E0x Filter On/Off : On the Amiga, this would set the enable (E00) or disable (E01) the analog 7 KHz low-pass filter on all channels. It has no effect in SharpMod.
        /// E1x: Fine (pitch) Slide Up
        /// E2x: Fine (pitch) Slide Down
        /// E3x: Glissando Control
        /// E4x: Vibrato Control
        /// E5x: Set Finetune
        /// E6x: Patternloop
        /// E7x: Tremolo Control
        /// E8x: Panning Control
        /// E9x: Retrig Note
        /// EAx: Fine Volume Slide Up
        /// EBx: Fine Volume Slide Down
        /// ECx: NoteCut
        /// EDx: NoteDelay
        /// EEx: PatternDelay
        /// EFx: Invert Loop (unsupported)
        /// See also: Pro Tracker Effect Commands. Original Amiga chipset - Audio features.
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoEEffects(short dat) //DoPTEffectE
        {
            short nib;

            dat &= 0xFF;

            nib = (short)(dat & 0xf);

            switch (dat >> 4)
            {
                //hardware filter toggle, not supported 
                case (short)(0x0):
                    break;

                //fineslide up
                case (short)(0x1):
                    if (TickCounter == 0)
                        a.TmpPeriod -= (nib << 2);
                    break;

                //fineslide dn
                case (short)(0x2):
                    if (TickCounter == 0)
                        a.TmpPeriod += (nib << 2);
                    break;

                //glissando ctrl
                case (short)(0x3):
                    a.Glissando = nib;
                    break;

                //set vibrato waveform
                case (short)(0x4):
                    a.WaveControl &= 0xf0;
                    a.WaveControl |= nib;
                    break;

                //set finetune
                case (short)(0x5):
                    break;

                //set patternloop
                case (short)(0x6):

                    if (TickCounter != 0)
                        break;

                    //hmm.. this one is a real kludge. But now it works.
                    if (nib != 0)
                    {
                        // set reppos or repcnt ? 

                        // set repcnt, so check if repcnt already is set,
                        // which means we are already looping 
                        if (RepeatCounter > 0)
                            // already looping, decrease counter
                            RepeatCounter--;
                        else
                            // not yet looping, so set repcnt
                            RepeatCounter = nib;


                        if (RepeatCounter != 0)
                            // jump to reppos if repcnt>0 
                            mp_patpos = PatternLoopPosition;
                    }
                    else
                    {
                        // set reppos 
                        PatternLoopPosition = (short)(mp_patpos - 1);
                    }
                    break;

                //set tremolo waveform
                case (short)(0x7):
                    a.WaveControl &= 0x0f;
                    a.WaveControl |= (short)(nib << 4);
                    break;

                //set panning 
                case (short)(0x8):
                    if (mp_panning)
                    {
                        nib <<= 4;
                        a.Panning = nib;
                        CurrentUniMod.Panning[mp_channel] = nib;
                    }
                    break;

                //retrig note
                case (short)(0x9):

                    if (nib > 0)
                    {
                        if (a.Retrig == 0)
                        {

                            // when retrig counter reaches 0,
                            // reset counter and restart the sample
                            a.Kick = true;
                            a.Retrig = (sbyte)nib;
                        }
                        // countdown
                        a.Retrig--;
                    }
                    break;

                //fine volume slide up
                case (short)(0xa):
                    if (TickCounter != 0)
                        break;

                    a.TmpVolume = (sbyte)(a.TmpVolume + nib);
                    if (a.TmpVolume > 64)
                        a.TmpVolume = 64;
                    break;

                //fine volume slide dn
                case (short)(0xb):
                    if (TickCounter != 0)
                        break;

                    a.TmpVolume = (sbyte)(a.TmpVolume - nib);
                    if (a.TmpVolume < 0)
                        a.TmpVolume = 0;
                    break;

                // cut note
                case (short)(0xc):

                    if (TickCounter >= nib)
                    {
                        // just turn the volume down
                        a.TmpVolume = 0;
                    }
                    break;

                //note delay
                case (short)(0xd):

                    if (TickCounter == nib)
                    {
                        a.Kick = true;
                    }
                    else
                        a.Kick = false;
                    break;

                //pattern delay
                case (short)(0xe):
                    if (TickCounter != 0)
                        break;

                    // only once (when vbtick=0)
                    if (SecondPatternDelayCounter == 0)
                        PatternDelayCounter = (short)(nib + 1);
                    break;

                //invert loop, not supported
                case (short)(0xf):
                    break;
            }
        }

        /// <summary>
        /// Set Speed/Tempo (MOD/XM: Fxx, S3M/IT:undefined)
        ///  This command can either set the speed (xx smaller than 20) or the tempo (xx greater than 20) of the song. 
        ///  Avoid using 20 as a parameter, since it can cause problem in some players. 
        ///  In MOD, F20 will set the SPEED of the song, but in XM, F20 will set the TEMPO (bpm) of the song. 
        ///  This value is in Hex.
        /// </summary>
        /// <param name="dat"></param>
        public virtual void DoPTEffectF(short dat)
        {
            if ((TickCounter != 0) || (SecondPatternDelayCounter != 0))
                return;

            if (mp_extspd && dat >= 0x20)
            {
                old_bpm = dat;

                mp_bpm = (short)rint(old_bpm * SpeedConstant);
            }
            else
            {
                if (dat != 0)
                {
                    // <- v0.44 bugfix
                    mp_sngspd = dat;
                    TickCounter = 0;
                }
            }
        }

        /// <summary>
        /// Set Speed (MOD/XM: undefined, S3M/IT:Axx)
        /// This command will set the speed of the current song (Hex). 
        /// Avoid using values bigger than 20, for better MOD/XM compatibility.
        /// </summary>
        /// <param name="speed"></param>
        public virtual void DoS3MSpeed(short speed)
        {
            speed &= 0xFF;

            if ((TickCounter != 0) || (SecondPatternDelayCounter != 0))
                return;

            if (speed != 0)
            {
                mp_sngspd = speed;
                TickCounter = 0;
            }
        }

        /// <summary>
        /// Set Tempo (MOD/XM: undefined, S3M/IT:Txx)
        /// This command will change the tempo of the song (Hex). 
        /// The minimum value is T20, and the maximum possible value is TFF. 
        /// The default tempo is 125 (T7D), which is equivalent to one tick every 20ms (50Hz)
        /// Note: T0x will decrease the current tempo by x. T1x will increase the current tempo by x.
        /// </summary>
        /// <param name="tempo"></param>
        public virtual void DoS3MTempo(short tempo)
        {
            tempo &= 0xFF;

            if ((TickCounter != 0) || (SecondPatternDelayCounter != 0))
                return;
            old_bpm = tempo;

            mp_bpm = (short)rint(old_bpm * SpeedConstant);
        }

        /// <summary>
        /// Tremor (MOD: undefined, XM: Txy, S3M/IT:Ixy)
        /// This effect will turn on and off the current channel every frame: T[ontime][offtime].
        /// x=ontime, y=offtime: the volume will stay unchanged for x frames, and then muted for y frames.
        /// Note: The exact duration of the ontime/offtime is different for MOD, XM and S3M/IT.
        /// </summary>
        /// <param name="inf"></param>
        public virtual void DoS3MTremor(short inf)
        {
            short on, off;

            inf &= 0xFF;

            if (inf != 0)
                a.S3mTrOnOff = inf;
            else
                inf = a.S3mTrOnOff;

            if (TickCounter == 0)
                return;

            on = (short)((inf >> 4) + 1);
            off = (short)((inf & 0xf) + 1);

            a.S3mTremor = (short)(a.S3mTremor % (on + off));
            a.Volume = (a.S3mTremor < on) ? a.TmpVolume : (sbyte)0;
            a.S3mTremor++;
        }

        /// <summary>
        /// Retrig Note(MOD:undefined, XM:Rxy, S3M/IT:Qxy)
        /// This command will retrig the same note before playing the next. 
        /// Where to retrig depends on the speed of the song. 
        /// If you retrig with 1 in speed 6 that note will be trigged 6 times in one note row. 
        /// Example:
        /// ... .. .. F06  (Set speed to 6)
        /// C-3 42 .. Q03  (Retrig at tick 3 out of 6)
        /// Retrig on hi-hats!
        /// </summary>
        /// <param name="inf"></param>
        public virtual void DoS3MRetrig(short inf)
        {
            short hi, lo;

            inf &= 0xFF;

            hi = (short)(inf >> 4);
            lo = (short)(inf & 0xf);

            if (lo != 0)
            {
                a.S3mRtgSlide = hi;
                a.S3mRtgSpeed = lo;
            }

            if (hi != 0)
            {
                a.S3mRtgSlide = hi;
            }

            // only retrigger if lo nibble > 0
            if (a.S3mRtgSpeed > 0)
            {
                if (a.Retrig == 0)
                {
                    // when retrig counter reaches 0,
                    // reset counter and restart the sample
                    a.Kick = true;
                    a.Retrig = (sbyte)a.S3mRtgSpeed;

                    // don't slide on first retrig 
                    if (TickCounter != 0)
                    {                        
                        switch (a.S3mRtgSlide)
                        {
                            case 1:
                            case 2:
                            case 3:
                            case 4:
                            case 5:
                                a.TmpVolume = (sbyte)(a.TmpVolume - (1 << (a.S3mRtgSlide - 1)));
                                break;

                            case 6:
                                a.TmpVolume = (sbyte)((2 * a.TmpVolume) / 3);
                                break;

                            case 7:
                                a.TmpVolume = (sbyte)(a.TmpVolume >> 1);
                                break;

                            case 9:
                            case (short)(0xa):
                            case (short)(0xb):
                            case (short)(0xc):
                            case (short)(0xd):
                                a.TmpVolume = (sbyte)(a.TmpVolume + (1 << (a.S3mRtgSlide - 9)));
                                break;

                            case (short)(0xe):
                                a.TmpVolume = (sbyte)((3 * a.TmpVolume) / 2);
                                break;

                            case (short)(0xf):
                                a.TmpVolume = (sbyte)(a.TmpVolume << 1);
                                break;
                        }
                        if (a.TmpVolume < 0)
                            a.TmpVolume = 0;
                        if (a.TmpVolume > 64)
                            a.TmpVolume = 64;
                    }
                }

                // countdown
                a.Retrig--;
            }
        }


        public virtual void PlayNote()
        {
            int period;
            Effects c;
            short inst;
            short note;

            if (a.Row == null)
                return;

            _uniTrack.UniSetRow(a.Row, a.RowPos);

            while ((c = (Effects)_uniTrack.UniGetByte()) != 0)
            {
                switch (c)
                {
                    case Effects.UNI_NOTE:
                        note = _uniTrack.UniGetByte();

                        if (note == 96)
                        {
                            /* key off ? */
                            a.KeyOn = false;
                            if ((a.Instrument != null) && ((a.Instrument.VolFlg & (short)EnvelopeFlags.EF_ON) == 0))
                            {
                                a.TmpVolume = 0;
                            }
                        }
                        else
                        {
                            a.Note = note;

                            period = GetPeriod((short)(note + a.Transpose), a.C2spd);

                            a.WantedPeriod = period;
                            a.TmpPeriod = period;

                            a.Kick = true;
                            a.Start = 0;

                            /* retrig tremolo and vibrato waves ? */
                            if ((a.WaveControl & 0x80) == 0)
                                a.TrmPos = 0;
                            if ((a.WaveControl & 0x08) == 0)
                                a.VibPos = 0;
                        }
                        break;

                    case Effects.UNI_INSTRUMENT:
                        inst = _uniTrack.UniGetByte();
                        if (inst >= CurrentUniMod.Instruments.Count)
                            break; /* <- safety valve */

                        a.SampleNumber = inst;

                        // i=&pf.instruments[inst];
                        a.Instrument = CurrentUniMod.Instruments[inst];

                        if (CurrentUniMod.Instruments[inst].SampleNumber[a.Note] >= CurrentUniMod.Instruments[inst].NumSmp)
                            break;

                        //s=&i.samples[i.samplenumber[a.note]];
                        a.Sample = CurrentUniMod.Instruments[inst].Samples[CurrentUniMod.Instruments[inst].SampleNumber[a.Note]];


                        /* channel or instrument determined panning ? */
                        if ((CurrentUniMod.Instruments[inst].Samples[CurrentUniMod.Instruments[inst].SampleNumber[a.Note]].Flags & (SampleFormatFlags.SF_OWNPAN)) != 0)
                        {
                            a.Panning = CurrentUniMod.Instruments[inst].Samples[CurrentUniMod.Instruments[inst].SampleNumber[a.Note]].Panning;
                        }
                        else
                        {
                            a.Panning = CurrentUniMod.Panning[mp_channel];
                        }

                        a.Transpose = CurrentUniMod.Instruments[inst].Samples[CurrentUniMod.Instruments[inst].SampleNumber[a.Note]].Transpose;
                        a.Handle = CurrentUniMod.Instruments[inst].Samples[CurrentUniMod.Instruments[inst].SampleNumber[a.Note]].Handle;
                        a.TmpVolume = (sbyte)(CurrentUniMod.Instruments[inst].Samples[CurrentUniMod.Instruments[inst].SampleNumber[a.Note]].Volume);
                        a.Volume = (sbyte)(CurrentUniMod.Instruments[inst].Samples[CurrentUniMod.Instruments[inst].SampleNumber[a.Note]].Volume);
                        a.C2spd = CurrentUniMod.Instruments[inst].Samples[CurrentUniMod.Instruments[inst].SampleNumber[a.Note]].C2Spd;
                        a.Retrig = 0;
                        a.S3mTremor = 0;

                        period = GetPeriod((short)(a.Note + a.Transpose), (short)a.C2spd);

                        a.WantedPeriod = period;
                        a.TmpPeriod = period;
                        break;

                    default:
                        _uniTrack.UniSkipOpcode((short)c);
                        break;

                }
            }
        }




        public virtual void PlayEffects()
        {
            Effects c;
            //short dat;

            if (a.Row == null)
                return;

            _uniTrack.UniSetRow(a.Row, a.RowPos);

            a.OwnPer = 0;
            a.OwnVol = 0;

            while ((c = (Effects)_uniTrack.UniGetByte()) != 0)
            {
                switch (c)
                {
                    case Effects.UNI_NOTE:
                    case Effects.UNI_INSTRUMENT:
                        _uniTrack.UniSkipOpcode((short)c);
                        break;

                    case Effects.UNI_PTEFFECT0:
                        DoPTEffect0(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECT1:
                        DoPTEffect1(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECT2:
                        DoPTEffect2(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECT3:
                        DoPTEffect3(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECT4:
                        DoPTEffect4(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECT5:
                        DoPTEffect5(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECT6:
                        DoPTEffect6(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECT7:
                        DoPTEffect7(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECT8:
                        DoPTEffect8(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECT9:
                        DoPTEffect9(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECTA:
                        DoVolSlide(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECTB:
                        DoPTEffectB(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECTC:
                        DoPTEffectC(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECTD:
                        DoPTEffectD(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECTE:
                        DoEEffects(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_PTEFFECTF:
                        DoPTEffectF(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_S3MEFFECTD:
                        DoS3MVolSlide(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_S3MEFFECTE:
                        DoS3MSlideDn(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_S3MEFFECTF:
                        DoS3MSlideUp(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_S3MEFFECTI:
                        DoS3MTremor(_uniTrack.UniGetByte());
                        a.OwnVol = 1;
                        break;

                    case Effects.UNI_S3MEFFECTQ:
                        DoS3MRetrig(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_S3MEFFECTA:
                        DoS3MSpeed(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_S3MEFFECTT:
                        DoS3MTempo(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_XMEFFECTA:
                        DoXMVolSlide(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_XMEFFECTG:
                        globalvolume = (sbyte)_uniTrack.UniGetByte();
                        break;

                    case Effects.UNI_XMEFFECTH:
                        DoXMGlobalSlide(_uniTrack.UniGetByte());
                        break;

                    case Effects.UNI_XMEFFECTP:
                        DoXMPanSlide(_uniTrack.UniGetByte());
                        break;

                    default:
                        _uniTrack.UniSkipOpcode((short)c);
                        break;

                }
            }

            if (a.OwnPer == 0)
            {
                a.Period = a.TmpPeriod;
            }

            if (a.OwnVol == 0)
            {
                a.Volume = a.TmpVolume;
            }
        }




        public static short InterpolateEnv(short p, EnvPt a, EnvPt b)
        {
            return (Interpolate(p, a.Pos, b.Pos, a.Val, b.Val));
        }


        public static short DoPan(short envpan, short pan)
        {
            return (short)(pan + (((envpan - 128) * (128 - System.Math.Abs(pan - 128))) / 128));
        }

        public static short DoVol(int a, short b, short c)
        {
            a *= b;
            a *= c;

            return (short)(a >> 23);
        }


        public static void StartEnvelope(EnvPr t, short flg, short pts, short sus, short beg, short end, EnvPt[] p)
        {
            flg &= 0xFF;
            pts &= 0xFF;
            sus &= 0xFF;
            beg &= 0xFF;
            end &= 0xFF;

            t.Flg = flg;
            t.Pts = pts;
            t.Sus = sus;
            t.Beg = beg;
            t.End = end;
            t.EnvPoints = p;
            t.CurrentCounter = 0;
            t.EnvIdxA = 0;
            t.EnvIdxB = 1;
        }



        public static short ProcessEnvelope(EnvPr t, short v, bool keyon)
        {
            /* panning active? . copy variables */
            if ((t.Flg & (short)EnvelopeFlags.EF_ON) != 0)
            {
                short a, b;
                int p;

                a = t.EnvIdxA;
                b = t.EnvIdxB;
                p = t.CurrentCounter;

                /* compute the envelope value between points a and b */
                v = InterpolateEnv((short)p, t.EnvPoints[a], t.EnvPoints[b]);

                /* Should we sustain? (sustain flag on, key-on, point a is the sustain
                point, and the pointer is exactly on point a) */
                if (((t.Flg & (short)EnvelopeFlags.EF_SUSTAIN) != 0) && keyon && a == t.Sus && p == t.EnvPoints[a].Pos)
                {
                    /* do nothing */
                }
                else
                {
                    /* don't sustain, so increase pointer. */
                    p++;

                    /* pointer reached point b? */
                    if (p >= t.EnvPoints[b].Pos)
                    {

                        /* shift points a and b */
                        a = b; b++;

                        if ((t.Flg & (short)EnvelopeFlags.EF_LOOP) != 0)
                        {
                            if (b > t.End)
                            {
                                a = t.Beg;
                                b = (short)(a + 1);
                                p = t.EnvPoints[a].Pos;
                            }
                        }
                        else
                        {
                            if (b >= t.Pts)
                            {
                                b--;
                                p--;
                            }
                        }
                    }
                }
                t.EnvIdxA = a;
                t.EnvIdxB = b;
                t.CurrentCounter = (short)p;
            }
            return v;
        }

        /*
        int GetFreq2(int period)
        {
        float frequency;
		
        frequency=8363.0*pow(2,((6*12*16*4.0)-period)/(12*16*4.0));
        return(floor(frequency));
        }*/

        public static int GetFreq2(int period)
        {
            int okt;
            int frequency;
            period = 7680 - period;
            okt = period / 768;
            frequency = lintab[period % 768];
            frequency <<= 2;
            return (frequency >> (7 - okt));
        }

        public virtual void MP_HandleTick()
        {
            int tmpvol;
            // extern char current_file[1024];
            //int z, t, tr;
            int t;
            ActionsEnum ui_result;
            //extern int play_current;
            // extern int current_pattern;
            //extern int count_pattern, count_song;
            bool reinit_audio = false;

            PauseFlag = -128;
            if (isfirst != 0)
            {
                // don't handle the very first ticks, this allows the
                // other hardware to settle down so we don't lose any 
                // starting notes                
                isfirst--;
                return;
            }

            if (forbid)
                return;

            // don't go any further when forbid is true
            if (MP_Ready())
                return;

            if (++TickCounter >= mp_sngspd)
            {
                mp_patpos++;
                TickCounter = 0;

                if (PatternDelayCounter != 0)
                {
                    SecondPatternDelayCounter = PatternDelayCounter;
                    PatternDelayCounter = 0;
                }

                if (SecondPatternDelayCounter != 0)
                {
                    // patterndelay active
                    if ((--SecondPatternDelayCounter) != 0)
                    {
                        // so turn back mp_patpos by 1
                        mp_patpos--;
                    }
                }

                // Do we have to get a new patternpointer ?
                // (when mp_patpos reaches 64 or when
                // a patternbreak is active). Also check for 256 - if mod 
                // is broken it will continue forever otherwise 
                if (mp_patpos == numrow || mp_patpos > 255)
                    posjmp = 3;

                if (posjmp != 0)
                {
                    mp_patpos = PatternBreakPosition;
                    mp_sngpos = (short)(mp_sngpos + (posjmp - 2));
                    PatternBreakPosition = (short)(posjmp = 0);
                    if (mp_sngpos >= CurrentUniMod.Positions.Count-1/* .NumPos*/)
                    {
                        /*				if(true) return;*/
                        if (!mp_loop)
                        {
                            if (OnCurrentModEnd != null)
                                OnCurrentModEnd();
                            return;
                        }
                        mp_sngpos = CurrentUniMod.RepPos;
                    }
                    if (mp_sngpos < 0)
                        mp_sngpos = (short)(CurrentUniMod.Positions.Count - 1);
                }


                if (SecondPatternDelayCounter == 0)
                {

                    for (t = 0; t < CurrentUniMod.ChannelsCount; t++)
                    {

                        //tr = CurrentUniMod.Patterns[(CurrentUniMod.Positions[mp_sngpos] * CurrentUniMod.NumChn) + t];
                        //Todo: Check for overflow at the end of the module

                        mp_channel = (short)t;
                        a = mp_audio[t];
                        if (CurrentUniMod.Positions[mp_sngpos] < CurrentUniMod.Patterns.Count)
                        {
                            numrow = CurrentUniMod.Patterns[CurrentUniMod.Positions[mp_sngpos]].RowsCount; //(short)(CurrentUniMod.PattRows[CurrentUniMod.Positions[mp_sngpos]]);
                            a.Row = CurrentUniMod.Patterns[CurrentUniMod.Positions[mp_sngpos]].Tracks[t].UniTrack;
                            a.RowPos = _uniTrack.UniFindRow(a.Row, mp_patpos);
                        }
                        else
                            a.Row = null;

                        //a.row = (tr<pf.numtrk) ? MikMod.MUniTrk.clMUniTrk.UniFindRow(pf.tracks[tr],mp_patpos) : ((short*)null);
                        /*if (tr < CurrentUniMod.NumTrk)
                        {
                            a.Row = CurrentUniMod.Tracks[tr];
                            a.RowPos = _uniTrack.UniFindRow(CurrentUniMod.Tracks[tr], mp_patpos);
                        }
                        else
                            a.Row = null;*/



                        PlayNote();
                    }

                    //run through once, repeat if paused
                    do
                    {
                        //don't need to eat cpu time!
                        if (PauseFlag == 127)
                            System.Threading.Thread.Sleep(1000);
                        //m_.usleep(1000);


                        //m_.UI.count_pattern++;
                        //m_.UI.count_song++;
                        //if (m_.quiet)
                        if (OnGetUIActions != null)
                            ui_result = OnGetUIActions();
                        else
                            ui_result = ActionsEnum.DEFAULT;
                        //* don't match any case */
                        //else
                        //    ui_result = m_.UI.get_ui();

                        /* volume=0 already by default if paused, so don't need to fiddle with it... */
                        switch (ui_result)
                        {
                            case ActionsEnum.UI_DELETE_MARKED:
                            /*if(!m_.cur_mod.deleted)
                            break;
                            if(!unlink(m_.cur_mod.filename))
                            m_.cur_mod.deleted=2;
                            else
                            m_.cur_mod.deleted=3;
                            m_.Display.update_file_display();
                            m_.Display.display_all(); */
                            /* FALL THROUGH */
                            case ActionsEnum.UI_NEXT_SONG:
                                //_driver.MD_PatternChange();
                                this.PlayCurrent = false;
                                break;

                            case ActionsEnum.UI_PREVIOUS_SONG:
                                //if ((m_.UI.count_song < MikMod.UI.myUI.SMALL_DELAY) && (m_.optind > 1))
                                //{
                                //    m_.optind -= 2;
                                //    this.play_current = false;
                                //}
                                //else
                                //{
                                //    mp_sngpos = 1;
                                //    MP_PrevPosition();
                                //}
                                //m_.UI.count_song = 0;
                                //_driver.MD_PatternChange();
                                break;

                            case ActionsEnum.UI_QUIT:
                                //_driver.MD_PatternChange();
                                Quit = true;
                                break;

                            case ActionsEnum.UI_JUMP_TO_NEXT_PATTERN:
                                // _driver.MD_PatternChange();
                                MP_NextPosition();
                                break;

                            case ActionsEnum.UI_JUMP_TO_PREV_PATTERN:
                                //_driver.MD_PatternChange();
                                //if (m_.UI.count_pattern < MikMod.UI.myUI.SMALL_DELAY)
                                ///* near start of pattern? */
                                //    MP_PrevPosition();
                                //else
                                //    MP_RestartPosition();
                                //m_.UI.count_pattern = 0;
                                break;

                            case ActionsEnum.UI_PAUSE:
                                PauseFlag = ~PauseFlag;
                                if (PauseFlag == 127)
                                {
                                    //if (m_.md_type != 0)
                                    //    _driver.MD_Mute();
                                    //else
                                    //_driver.MD_Exit();
                                    /* temp. free the sound driver */
                                    /*m_.Display.display_version();
                                    m_.Display.display_pausebanner();*/
                                }
                                else
                                {
                                    //if (m_.md_type != 0)
                                    //{
                                    //    _driver.MD_UnMute();
                                    //    m_.Display.display_all();
                                    //}
                                    ///* need to re-init. the sound driver before leaving pause mode */
                                    //else
                                    //{
                                    /*if (!_driver.MD_Init())
                                    {                                       
                                        
                                        PauseFlag = ~PauseFlag;
                                    }*/
                                    /*else
                                        m_.Display.display_all();*/
                                    //}
                                }
                                break;

                            case ActionsEnum.UI_SPEED_UP:
                                if ((old_bpm * (SpeedConstant + 0.05)) <= 255)
                                    SpeedConstant = (float)(SpeedConstant + 0.05);
                                break;

                            case ActionsEnum.UI_SLOW_DOWN:
                                if ((old_bpm * (SpeedConstant - 0.05)) > 10)
                                    SpeedConstant = (float)(SpeedConstant - 0.05);
                                break;

                            case ActionsEnum.UI_NORMAL_SPEED:
                                SpeedConstant = 1.0f;
                                break;

                            case ActionsEnum.UI_VOL_UP:
                                if (mp_volume < 250)
                                    mp_volume = (short)(mp_volume + 5);
                                break;

                            case ActionsEnum.UI_VOL_DOWN:
                                if (mp_volume > 5)
                                    mp_volume = (short)(mp_volume - 5);
                                break;

                            case ActionsEnum.UI_NORMAL_VOL:
                                mp_volume = 100;
                                break;

                            case ActionsEnum.UI_MARK_DELETED:
                                /*if (!m_.cur_mod.Deleted)
                                    m_.cur_mod.Deleted = true;
                                else if (m_.cur_mod.Deleted == true)
                                    m_.cur_mod.Deleted = false;
                                m_.Display.update_file_display();
                                m_.Display.display_all();*/
                                break;

                            case ActionsEnum.UI_SELECT_STEREO:
                                //_driver.md_mode |= DMode.DMODE_STEREO;
                                reinit_audio = true;
                                break;

                            case ActionsEnum.UI_SELECT_MONO:
                                //_driver.md_mode &= ~DMode.DMODE_STEREO;
                                reinit_audio = true;
                                break;

                            case ActionsEnum.UI_SELECT_INTERP:
                                //_driver.md_mode |= DMode.DMODE_INTERP;
                                reinit_audio = true;
                                break;

                            case ActionsEnum.UI_SELECT_NONINTERP:
                                //_driver.md_mode &= ~DMode.DMODE_INTERP;
                                reinit_audio = true;
                                break;

                            case ActionsEnum.UI_SELECT_8BIT:
                                //_driver.md_mode &= ~DMode.DMODE_16BITS;
                                reinit_audio = true;
                                break;

                            case ActionsEnum.UI_SELECT_16BIT:
                                //_driver.md_mode |= DMode.DMODE_16BITS;
                                reinit_audio = true;
                                break;

                            default:
                                break;

                        }
                        if ((old_bpm * SpeedConstant) > 255)
                            mp_bpm = 255;
                        else
                            mp_bpm = (short)rint(old_bpm * SpeedConstant);

                        if (reinit_audio)
                        {
                            reinit_audio = false;
                            /*_driver.MD_Exit();
                            _driver.MD_Init();*/
                        }
                    }
                    while (PauseFlag == 127);
                }

            }

            /* Update effects */
            for (t = 0; t < CurrentUniMod.ChannelsCount; t++)
            {
                mp_channel = (short)t;
                a = mp_audio[t];
                PlayEffects();
            }

            for (t = 0; t < CurrentUniMod.ChannelsCount; t++)
            {
                //INSTRUMENT *i;
                //SAMPLE *s;
                short envpan, envvol;

                a = mp_audio[t];
                //i=a.i;
                //s=a.s;

                if (a.Instrument == null || a.Sample == null)
                    continue;

                if (a.Period < 40)
                    a.Period = 40;
                if (a.Period > 8000)
                    a.Period = 8000;

                if (a.Kick)
                {
                    _mixer.VC_VoicePlay((short)t, a.Handle, a.Start, a.Sample.Length, a.Sample.LoopStart, a.Sample.LoopEnd, a.Sample.Flags);
                    a.Kick = false;
                    a.KeyOn = true;

                    a.FadeVol = 32768;

                    StartEnvelope(a.VolEnv, a.Instrument.VolFlg, a.Instrument.VolPts, a.Instrument.VolSus, a.Instrument.VolBeg, a.Instrument.VolEnd, a.Instrument.VolEnv);
                    StartEnvelope(a.PanEnv, a.Instrument.PanFlg, a.Instrument.PanPts, a.Instrument.PanSus, a.Instrument.PanBeg, a.Instrument.PanEnd, a.Instrument.PanEnv);
                }

                envvol = ProcessEnvelope(a.VolEnv, (short)256, a.KeyOn);
                envpan = ProcessEnvelope(a.PanEnv, (short)128, a.KeyOn);

                tmpvol = a.FadeVol; /* max 32768 */
                tmpvol *= envvol; /* * max 256 */
                tmpvol *= a.Volume; /* * max 64 */
                tmpvol /= 16384; /* tmpvol/(256*64) => tmpvol is max 32768 */

                tmpvol *= globalvolume; /* * max 64 */
                tmpvol *= mp_volume; /* * max 100 */
                tmpvol /= 3276800; /* tmpvol/(64*100*512) => tmpvol is max 64 */

                _mixer.VC_VoiceSetVolume((short)t, (short)tmpvol);
                // _driver.MD_VoiceSetVolume(t,tmpvol&0xFF);

                if ((a.Sample.Flags & (SampleFormatFlags.SF_OWNPAN)) != 0)
                {
                    _mixer.VC_VoiceSetPanning((short)t, DoPan(envpan, a.Panning));
                    // _driver.MD_VoiceSetPanning(t,DoPan(envpan,a.panning) & 0xFF);
                }
                else
                {
                    _mixer.VC_VoiceSetPanning((short)t, a.Panning);
                    // _driver.MD_VoiceSetPanning(t,(a.panning) & 0xFF);
                }

                if ((CurrentUniMod.Flags & UniModFlags.UF_LINEAR) != 0)
                    _mixer.VC_VoiceSetFrequency((short)t, GetFreq2(a.Period));
                else
                    _mixer.VC_VoiceSetFrequency((short)t, (3579546 << 2) / a.Period);
                //_driver.MD_VoiceSetFrequency((short)t, (3579546 << 2) / a.Period);

                /*  if key-off, start substracting
                fadeoutspeed from fadevol: */
                if (!a.KeyOn)
                {
                    if (a.FadeVol >= a.Instrument.VolFade)
                        a.FadeVol -= a.Instrument.VolFade;
                    else
                        a.FadeVol = 0;
                }
            }


        }

        public virtual void MP_Init(SongModule m)
        {
            int t;

            CurrentUniMod = m;
            PatternLoopPosition = 0;
            RepeatCounter = 0;
            mp_sngpos = 0;
            mp_sngspd = m.InitialSpeed;

            TickCounter = mp_sngspd;
            PatternDelayCounter = 0;
            SecondPatternDelayCounter = 0;
            mp_bpm = m.InitialTempo;
            old_bpm = mp_bpm;
            //m_.cur_mod.Deleted = false;

            forbid = false;
            mp_patpos = 0;
            posjmp = 2; /* <- make sure the player fetches the first note */
            PatternBreakPosition = 0;

            isfirst = 2; /* delay start by 2 ticks */

            globalvolume = 64; /* reset global volume */

            /* Make sure the player doesn't start with garbage: */
            for (t = 0; t < CurrentUniMod.ChannelsCount; t++)
            {
                mp_audio[t].Kick = false;
                mp_audio[t].TmpVolume = 0;
                mp_audio[t].Retrig = 0;
                mp_audio[t].WaveControl = 0;
                mp_audio[t].Glissando = 0;
                mp_audio[t].SampleOffset = 0;
            }
        }

        public virtual bool MP_Ready()
        {
            return (mp_sngpos >= CurrentUniMod.Positions.Count);
        }

        public virtual void MP_NextPosition()
        {
            forbid = true;
            posjmp = 3;
            PatternBreakPosition = 0;
            TickCounter = mp_sngspd;
            forbid = false;
        }

        public virtual void MP_PrevPosition()
        {
            forbid = true;
            posjmp = 1;
            PatternBreakPosition = 0;
            TickCounter = mp_sngspd;
            forbid = false;
        }

        public virtual void MP_RestartPosition()
        {
            forbid = true;
            posjmp = 2;
            PatternBreakPosition = 0;
            TickCounter = mp_sngspd;
            forbid = false;
        }

        public virtual void MP_SetPosition(short pos)
        {
            /* avoid infinitely-looping mods */

            /*	if(pos>=pf.numpos) pos=pf.numpos;
            forbid=true;
            posjmp=2;
            patbrk=0;
            mp_sngpos=pos; 
            vbtick=mp_sngspd;
            forbid=false;*/
        }

        public static double rint(double x)
        {
            return Math.Round(x);
        }
    }
}