/** Perform one flip. Execution resumes after its native action cost completes. */
declare function doAFlip(): void;

/** Whether the entity under the drone is ready to harvest. Does not harvest it. */
declare function canHarvest(): boolean;
