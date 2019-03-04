namespace AutoAudio
{
    /// <summary>Class AutoAudioOptions stores the options for AutoAudio. This file is serialized to disk.</summary>
    class AutoAudioOptions
    {
        /// <summary>Gets or sets the hour when night time(see <em>NightTimeDontSwitchFromThisAudioDevice</em>) starts.</summary>
        /// <value>The night time start hour(0-23).</value>
        public int NightTimeStart { get; set; }
        /// <summary>Gets or sets the hour when night time(see <em>NightTimeDontSwitchFromThisAudioDevice</em>) ends.</summary>
        /// <value>The night time end hour(0-23).</value>
        public int NightTimeEnd { get; set; }
        /// <summary>
        /// Gets or sets the night time audio device. If this audio devices is active during night time hours(see <em>NightTimeStart</em> and <em>NightTimeEnd</em>) then the default audio device is not switched back to the other device. This was implemented to prevent accidental switching to speakers and blasting louds music during the night.
        /// </summary>
        /// <value>The night time audio device.</value>
        public string NightTimeDontSwitchFromThisAudioDevice { get; set; }

        /// <summary>Gets or sets the version.</summary>
        /// <value>The version.</value>
        public int Version { get; set; }
        /// <summary>Gets or sets the first audio device1</summary>
        /// <value>The first audio device.</value>
        public string AudioDevice1 { get; set; }
        /// <summary>Gets or sets the second audio device.</summary>
        /// <value>The second audio device.</value>
        public string AudioDevice2 { get; set; }
        /// <summary>Gets or sets the process name of the second audio device. When a process with this name is running, the default audio device is switched to <em>AudioDevice2</em></summary>
        /// <value>The process name of the second audio device.</value>
        public string ProcessAudioDevice2 { get; set; }
    }
}