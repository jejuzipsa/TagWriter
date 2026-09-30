# TagWriter architecture draft

TagWriter is a Windows-first novel-writing workspace. Its central concept is that tags connect manuscript text, setting cards and a visual relationship map.

## Core workflow
1. Create character/location/item/event cards.
2. Give each card aliases (tags).
3. Card nodes automatically exist in the mind map.
4. The writer manually arranges nodes and describes meaningful relationships on edges.
5. While writing, aliases are detected in manuscript text.
6. Occurrences can be reviewed from the entity panel.
7. Co-occurrence can later create suggested/automatic edges; user-edited relationships are preserved.
8. Ideas can be captured separately and linked to cards/chapters.
9. The same JSON is intended to be readable by a future lightweight web editor/viewer.

## Document hierarchy
Project → Chapter → Scene → Content. Scenes are logical editing/reordering units. 전체 원고 is a continuous reading view assembled from all chapters/scenes.

## Project file
A project is one UTF-8 JSON file. It contains format, formatVersion, revision, project metadata, chapters, cards, mindmap, ideas and settings. Manuscript content remains ordinary text (Markdown-compatible by convention). Tag markup is not injected into prose.

## Entity model
Initial card types are Character, Location, Item and Event. Every card has a stable ID. Display names and aliases may change without breaking relationships.

## Relationship model
Mind-map nodes are created from cards and their positions are persistent. Edges contain from/to IDs, an editable label, a source (manual or later auto), and a userEdited flag. Automatic relationships are suggestions derived from manuscript co-occurrence. A user-edited edge must not disappear just because the originating prose changes.

## Desktop layout
Traditional productivity layout: top menu, left project tree, center editor/mind-map workspace, right contextual panel and bottom status bar. Dark theme first, light theme later.

## Web direction
A future TagWriter Web should use the same JSON and focus on outside-the-house tasks: reading the manuscript, capturing ideas, editing cards/aliases and editing relationship notes.

## Export direction
Planned exports: DOCX, HWP/HWPX, PDF, HTML and Markdown. Exported manuscript should omit editor-only tag decoration by default.
