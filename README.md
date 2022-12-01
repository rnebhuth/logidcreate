# logidcreate
Creates unique Log Ids for c# Projects

1. Use import command to generate *EventId.cs files in all projects of the targeted solution
2. Adapt the GlobalEventId.cs files with the desired base EventIds for each project.
3. Use createid command to analyze logging method invocations and to create/update EventIds in *EventId.cs for each logger invocation. 
This will also change every line of the code invoking affected logger methods to use the constants from *EventId.cs files.
 TODO:
