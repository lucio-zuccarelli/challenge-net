<template>
  <div>
    <h2>Pacientes</h2>
    <table v-if="pacientes.length">
      <thead>
        <tr>
          <th>#</th>
          <th>Nombre</th>
          <th>DNI</th>
          <th>Email</th>
          <th>Teléfono</th>
          <th>No-shows</th>
          <th>Estado</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="p in pacientes" :key="p.id">
          <td>{{ p.id }}</td>
          <td>{{ p.nombreCompleto }}</td>
          <td>{{ p.dni }}</td>
          <td>{{ p.email }}</td>
          <td>{{ p.telefono }}</td>
          <td>{{ p.noShowCount }}</td>
          <td>
            <span v-if="p.bloqueado" style="color: #d32f2f; font-weight: 600">
              Bloqueado hasta {{ fechaDesbloqueo(p.fechaBloqueo) }}
            </span>
            <span v-else style="color: #388e3c">Activo</span>
          </td>
          <td style="display: flex; gap: 8px; align-items: center">
            <button v-if="p.bloqueado" @click="desbloquear(p.id)">Desbloquear</button>
            <button class="btn-danger" @click="eliminar(p.id)">Eliminar</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else>No hay pacientes registrados.</p>
  </div>
</template>

<script>
import { pacientesApi } from '../services/api'

export default {
  name: 'PacientesList',
  data() {
    return {
      pacientes: []
    }
  },
  async mounted() {
    try {
      const res = await pacientesApi.getAll()
      this.pacientes = res.data
    } catch {
      alert('Error al procesar la solicitud')
    }
  },
  methods: {
    fechaDesbloqueo(fechaBloqueo) {
      if (!fechaBloqueo) return 'fecha indefinida'
      const d = new Date(fechaBloqueo)
      d.setDate(d.getDate() + 30)
      return d.toLocaleDateString('es-AR')
    },
    async desbloquear(id) {
      if (!confirm('¿Confirma que desea desbloquear este paciente?')) return
      try {
        const res = await pacientesApi.desbloquear(id)
        const idx = this.pacientes.findIndex(p => p.id === id)
        if (idx !== -1) this.pacientes[idx] = res.data
      } catch (err) {
        alert(err.response?.data?.mensaje || 'Error al desbloquear el paciente.')
      }
    },
    async eliminar(id) {
      if (!confirm('¿Confirma que desea eliminar este paciente? Esta acción no se puede deshacer.')) return
      try {
        await pacientesApi.delete(id)
        this.pacientes = this.pacientes.filter(p => p.id !== id)
      } catch (err) {
        alert(err.response?.data?.mensaje || 'Error al eliminar el paciente.')
      }
    }
  }
}
</script>
