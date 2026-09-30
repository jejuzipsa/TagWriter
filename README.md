# TagWriter

**A novel writing app that connects your story, characters, settings, and ideas through tags and interactive mind maps.**

TagWriter is a Windows-first writing workspace for planning and drafting fiction. Cards define characters, locations, items and events; aliases become manuscript tags; those same cards become nodes in a relationship map.

## Prototype status
The repository contains the first .NET 8 / WPF prototype.

Implemented:
- chapter → scene manuscript structure
- continuous 전체 원고 reading view
- character / location / item / event cards
- aliases/tags stored separately from manuscript text
- simple occurrence lookup across scenes
- card nodes automatically represented in mind-map data
- persistent mind-map node/edge data model
- idea collection
- single-file UTF-8 JSON project format with format version + revision
- open/save JSON projects
- dark desktop layout with project tree, editor, context panel and status bar

The graph UI is intentionally basic. Dragging nodes, creating/editing labeled edges, automatic co-occurrence relationships, rich tag highlighting and export are next-stage work.

## Run
Requirements: Windows 10/11 and .NET 8 SDK.

    dotnet run

See docs/ARCHITECTURE.md for the current design.
