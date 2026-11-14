/** Perform one flip. Execution resumes after its native action cost completes. */
declare function doAFlip(): void;

/** Whether the entity under the drone is ready to harvest. Does not harvest it. */
declare function canHarvest(): boolean;

/** Harvest the entity under the drone; returns false if none exists.
 * Check canHarvest() first to avoid harvesting before it is ready.
 * Execution resumes after the native action cost completes.
 */
declare function harvest(): boolean;
