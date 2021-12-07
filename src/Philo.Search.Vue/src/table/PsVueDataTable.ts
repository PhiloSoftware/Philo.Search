import { VueDatatable } from "vuejs-datatable";

// eslint-disable-next-line @typescript-eslint/ban-types
export default class PsVueDataTable<TRow extends {}> extends VueDatatable<
  TRow,
  PsVueDataTable<TRow>
> {}
