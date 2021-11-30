import Table from "./Table.vue";

export default {
  component: Table,
  title: "Components/Table",
};

const Template = (args, { argTypes }) => ({
  props: Object.keys(argTypes),
  components: { Table },
  template: "<Table :tableId='tableId'></my-button>",
});

export const basic = Template.bind({});
basic.args = {
  tableId: "yolo",
};
