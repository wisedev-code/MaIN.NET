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
        Console.WriteLine("Piano Learning Assistant with Verified Educational Knowledge");

        var context = await AIHub.Agent()
            .WithModel("gemma3:4b")
            .WithMemoryParams(new MemoryParams() { ContextSize = 2137 })
            .WithInitialPrompt("""
                               You are an expert piano instructor specializing in teaching specific pieces,
                               techniques, and solving common playing problems. Help students learn exact
                               fingerings, chord progressions, and troubleshoot technical issues with
                               detailed, step-by-step guidance for both classical and popular music.
                               """)
            .WithKnowledge(KnowledgeBuilder.Instance
                .AddUrl("fundamentals_piano_practice", "https://fundamentals-of-piano-practice.readthedocs.io/",
                    tags: ["advanced_technique", "practice_methods", "memorization", "mental_play", "problem_solving"])
                .AddUrl("piano_practice_org", "http://www.pianopractice.org/",
                    tags: ["practice_strategies", "technique_acquisition", "chromatic_scales", "piano_tuning"])
                .AddUrl("piano_nanny_lessons", "https://pianonanny.com/page1.html",
                    tags: ["beginner_fundamentals", "keyboard_layout", "staff_notation", "octaves", "basic_theory"])
                .AddUrl("music2me_guide", "https://music2me.com/en/magazine/learn-piano-in-13-steps",
                    tags: ["structured_learning", "posture", "finger_positioning", "note_reading", "rhythm"])
                .AddUrl("8notes_theory", "https://www.8notes.com/theory/",
                    tags: ["music_theory", "staff_notation", "harmonic_analysis", "47_lesson_course"])
                .AddUrl("simplifying_theory",
                    "https://www.simplifyingtheory.com/how-to-read-sheet-music-for-beginners/",
                    tags: ["sheet_music_reading", "staff_fundamentals", "note_positioning", "18_lesson_series"])
                .AddUrl("music_theory_academy", "https://www.musictheoryacademy.com/",
                    tags: ["comprehensive_theory", "pitch", "clefs", "rhythms", "harmony", "composition"])
                .AddUrl("piano_chord_org", "https://www.pianochord.org/",
                    tags: ["chord_construction", "fingering_instructions", "harmonic_relationships", "chord_types"])
                .AddUrl("pianote_music_theory", "https://www.pianote.com/blog/piano-music-theory/",
                    tags: ["practical_theory", "chord_formulas", "extensions", "inversions", "voicings"])
                .AddUrl("pianote_notes_guide", "https://www.pianote.com/blog/how-to-read-piano-notes/",
                    tags: ["note_reading", "beginner_guide", "staff_reading", "piano_notation"])
                .AddUrl("piano_exercises", "https://pianoexercises.org/",
                    tags: ["classical_exercises", "czerny", "hanon", "clementi", "technique_studies"])
                .AddUrl("hanon_online", "https://www.hanon-online.com/",
                    tags: ["finger_exercises", "240_exercises", "finger_independence", "daily_practice"])
                .AddUrl("piano_scales", "https://www.pianoscales.org/major.html",
                    tags: ["scale_fingerings", "major_scales", "keyboard_diagrams", "interval_relationships"])
                .AddUrl("fun_piano_studio", "https://www.myfunpianostudio.com/music-theory/piano-theory-worksheets/",
                    tags: ["printable_worksheets", "theory_resources", "finger_numbers", "key_identification"])
                .AddUrl("liberty_park_terms", "https://www.libertyparkmusic.com/musical-terms-learning-piano/",
                    tags: ["musical_terms", "glossary", "100_definitions", "piano_vocabulary"])
                .AddUrl("8notes_main", "https://www.8notes.com/",
                    tags: ["free_sheet_music", "educational_scores", "practice_pieces", "music_resources"])
                .AddUrl("musopen", "https://musopen.org/",
                    tags: ["public_domain_music", "classical_scores", "royalty_free", "educational_library"]))
            .WithSteps(StepBuilder.Instance
                .AnswerUseKnowledge()
                .Build())
            .CreateAsync();

        var result = await context
            .ProcessAsync(
                "I want to learn basic major scales on piano. What's the correct fingering for C major scale?");

        Console.WriteLine(result.Message.Content);
    }
}