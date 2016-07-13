﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using BundtBot.BundtBot.Utility;
using Discord;
using Discord.Audio;

namespace BundtBot.BundtBot.Sound {
    class SoundManager {
        internal bool HasThingsInQueue => _soundQueue.Count > 0;
        internal bool IsPlaying { get; private set; }
        /// <summary> The voice channel that we are currently streaming to, if any. </summary>
        internal Channel VoiceChannel { get; private set; }
        public bool Shutdown { get; set; } = false;

        ConcurrentQueue<Sound> _soundQueue = new ConcurrentQueue<Sound>();
        readonly SoundStreamer _audioStreamer = new SoundStreamer();

        public SoundManager() {
            new Thread(async() => {
                while (true) {
                    if (Shutdown) break;

                    // Pick something from queue
                    if (_soundQueue.Count == 0) {
                        IsPlaying = false;
                        Thread.Sleep(100);
                        continue;
                    }

                    IsPlaying = true;

                    Sound sound;
                    var result = _soundQueue.TryDequeue(out sound);
                    if (result == false) { continue; }

                    MyLogger.WriteLine("Connecting to voice channel:" + sound.VoiceChannel.Name);
                    MyLogger.WriteLine("\tOn server:  " + sound.VoiceChannel.Server.Name);
                    var audioService = sound.VoiceChannel.Client.GetService<AudioService>();
                    var audioClient = await audioService.Join(sound.VoiceChannel);
                    VoiceChannel = sound.VoiceChannel;
                    await sound.TextChannel.SendMessage($"Playing {sound.SoundFile.Name} at Volume {sound.Volume * 10}");

                    _audioStreamer.PlaySound(audioService, audioClient, sound);

                    if (sound.DeleteAfterPlay) {
                        MyLogger.WriteLine("Deleting sound file: " + sound.SoundFile, ConsoleColor.Yellow);
                        sound.SoundFile.Delete();
                    }

                    // Check if next sound is in same channel
                    Sound nextSound;
                    if (_soundQueue.TryPeek(out nextSound)) {
                        if (nextSound.VoiceChannel == sound.VoiceChannel) {
                            continue;
                        }
                    }
                    
                    await audioService.Leave(sound.VoiceChannel);
                    VoiceChannel = null;
                    Thread.Sleep(250);
                }
            }).Start();
        }

        public async void EnqueueSound(Sound sound, bool sendTextUpdates = true) {
            Message msg = null;
            if (sendTextUpdates) {
                msg = await sound.TextChannel.SendMessage("Adding sound to the queue...");
            }
            _soundQueue.Enqueue(sound);
            MyLogger.WriteLine("[SoundManager] Sound queued: " + sound.SoundFile.Name);
            if (sendTextUpdates) {
                await msg.Edit(msg.Text + "done!");
            }
        }

        internal void Stop() {
            _soundQueue = new ConcurrentQueue<Sound>();
            _audioStreamer.Stop = true;
            MyLogger.WriteLine("[SoundManager] Stopped");
        }

        internal void Skip() {
            _audioStreamer.Stop = true;
            MyLogger.WriteLine("[SoundManager] Skipped");
        }

        internal void SetVolume(float desiredVolume) {
            _audioStreamer.SetVolume(desiredVolume);
            MyLogger.WriteLine("[SoundManager] Volume set to " + desiredVolume);
        }
    }
}
