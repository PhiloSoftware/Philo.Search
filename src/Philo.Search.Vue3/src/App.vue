<script setup lang="ts">

</script>

<template>
  App Main<br />
  <ol>
    <li><router-link to="/readme">Read Me</router-link></li>
    <li><router-link to="/table?tbl_page=2&tbl_pagesize=15&tbl_lastname_a=Like&tbl_lastname_v=sdf">Table</router-link></li>
  </ol>
  <router-view></router-view>
</template>

<style>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  margin-top: 60px;
}
</style>
