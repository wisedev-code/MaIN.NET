using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using Microsoft.Identity.Client;

namespace Examples.Agents;

public class AgentWithKnowledgeWebExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Piano Learning Assistant with Focused Knowledge Sources");

        AIHub.Extensions.DisableLLamaLogs();
        var context = await AIHub.Agent()
            .WithModel("llama3.2:3b")
            .WithMemoryParams(new MemoryParams(){ContextSize = 4096})
            .WithInitialPrompt("""
                               You are an expert piano instructor specializing in teaching specific pieces,
                               techniques, and solving common playing problems. Help students learn exact
                               fingerings, chord progressions, and troubleshoot technical issues with
                               detailed, step-by-step guidance for both classical and popular music.
                               """)
            .WithKnowledge(KnowledgeBuilder.Instance
                .AddUrl("piano_scales_major", "https://www.pianoscales.org/major.html",
                    tags: ["scale_fingerings", "c_major_scale", "d_major_scale", "fingering_patterns"])
                .AddUrl("piano_chord_database", "https://www.pianochord.org/",
                    tags: ["chord_fingerings", "cmaj7_chord", "chord_inversions", "left_hand_chords"])
                .AddUrl("fundamentals_practice_book", "https://fundamentals-of-piano-practice.readthedocs.io/",
                    tags: ["memorization_techniques", "mental_play_method", "practice_efficiency", "difficult_passages"])
                .AddUrl("hanon_exercises", "https://www.hanon-online.com/",
                    tags: ["hanon_exercises", "finger_independence", "daily_technical_work", "exercise_1_through_20"])
                .AddUrl("sheet_music_reading",
                    "https://www.simplifyingtheory.com/how-to-read-sheet-music-for-beginners/",
                    tags: ["bass_clef_reading", "treble_clef_notes", "note_identification", "staff_reading_speed"])
                .AddUrl("piano_fundamentals", "https://music2me.com/en/magazine/learn-piano-in-13-steps",
                    tags: ["proper_posture", "finger_numbering", "hand_position", "keyboard_orientation"])
                .AddUrl("theory_lessons", "https://www.8notes.com/theory/",
                    tags: ["interval_identification", "key_signatures", "circle_of_fifths", "time_signatures"])
                .AddUrl("piano_terms", "https://www.libertyparkmusic.com/musical-terms-learning-piano/",
                    tags: ["dynamics_markings", "tempo_markings", "articulation_symbols", "expression_terms"]))
            .WithSteps(StepBuilder.Instance
                .AnswerUseKnowledge()
                .Build())
            .CreateAsync();

        var result = await context
            .ProcessAsync("I want to learn the C major scale. What's the exact fingering pattern for both hands?" + "I want short and concrete answer");

        Console.WriteLine(result.Message.Content);
    }
}