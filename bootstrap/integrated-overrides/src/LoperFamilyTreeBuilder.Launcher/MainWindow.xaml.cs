using System.Windows;
using System.Windows.Controls;

namespace LoperFamilyTreeBuilder.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string section)
        {
            return;
        }

        SectionTitle.Text = section;
        WorkspaceHeading.Text = section;
        WorkspaceDescription.Text = section switch
        {
            "People" => "Native desktop people workspace. The existing genealogy services and Person IDs will be wired into this surface as the desktop migration continues.",
            "Person Profile" => "Single-window native person command profile for identity, family, timeline, media, health, sources, locations, and research.",
            "Family Tree" => "Native desktop family-tree workspace. Legacy Numbers remain read-only historical identifiers and are never recalculated.",
            "Relationships" => "Native relationship management for parent/child and family-union records with integrity safeguards.",
            "Lifetime Timeline" => "Chronological native desktop view for dated and approximate life events, evidence, and locations.",
            "Maps & Migration" => "Native desktop geography workspace for historical places, coordinates, residences, movement, and migration evidence.",
            "Photos" => "Native media archive for photographs, captions, people links, provenance, EXIF, and GPS metadata.",
            "Documents" => "Native document archive connected to people, events, sources, and AI transcription records.",
            "Sources" => "Native source and citation workspace preserving repository details, citations, confidence, and provenance.",
            "Medical & Health" => "Permission-aware native medical and family-health workspace. Medical records remain separate from Legacy Numbers.",
            "DNA Clusters" => "Protected native DNA match workspace. Evidence clustering does not create genealogical relationships automatically.",
            "AI Transcription" => "Native review workspace for document transcription drafts, corrections, confidence, and approval.",
            "Tree Integrity" => "Native review center for genealogy consistency findings. Findings never silently rewrite historical data.",
            "Backup & Restore" => "Desktop backup and restore center for the configured database, media archive, and network-drive copies.",
            "Settings" => "Desktop configuration for archive paths, backup paths, permissions, and application preferences.",
            _ => "Native Windows desktop workspace. Browser hosting is not required for the local application."
        };
    }
}
