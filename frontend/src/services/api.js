import axios from 'axios'

const BASE_URL = import.meta.env.VITE_API_BASE_URL

export const turnosApi = {
  getAll:           ()          => axios.get(`${BASE_URL}/turnos`),
  getById:          (id)        => axios.get(`${BASE_URL}/turnos/${id}`),
  create:           (data)      => axios.post(`${BASE_URL}/turnos`, data),
  cancelar:         (id)        => axios.post(`${BASE_URL}/turnos/${id}/cancelar`),
  marcarAusencia:   (id)        => axios.post(`${BASE_URL}/turnos/${id}/ausencia`),
  actualizarEstado: (id, data)  => axios.put(`${BASE_URL}/turnos/${id}/estado`, data)
}

export const pacientesApi = {
  getAll:      ()          => axios.get(`${BASE_URL}/pacientes`),
  getById:     (id)        => axios.get(`${BASE_URL}/pacientes/${id}`),
  create:      (data)      => axios.post(`${BASE_URL}/pacientes`, data),
  update:      (id, data)  => axios.put(`${BASE_URL}/pacientes/${id}`, data),
  delete:      (id)        => axios.delete(`${BASE_URL}/pacientes/${id}`),
  desbloquear: (id)        => axios.post(`${BASE_URL}/pacientes/${id}/desbloquear`)
}

export const medicosApi = {
  getAll: () => axios.get(`${BASE_URL}/medicos`)
}

export const sucursalesApi = {
  getAll: () => axios.get(`${BASE_URL}/sucursales`)
}

export const configuracionApi = {
  getPoliticaNoShow: () => axios.get(`${BASE_URL}/configuracion/politica-noshow`)
}
